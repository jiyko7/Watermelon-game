using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("---------------------[Core]")]
    public int maxLevel;
    public int score;
    public bool isOver;

    [Header("---------------------[Object Pooling]")]
    public Dongle lastDongle;
    public GameObject donglePrefab;
    public Transform dongleGroup;
    public GameObject effectPrefab;
    public Transform effectGroup;
    public List<Dongle> donglePool;
    public List<ParticleSystem> effectPool;
    [Range(1, 30)] // poolSize를 0에서 30까지 슬라이더로 조정가능
    public int poolSize;
    public int poolCursor;

    [Header("---------------------[Audio]")]
    public AudioSource bgmPlayer;
    public AudioSource[] sfxPlayer;
    public AudioClip[] sfxClip;
    public enum Sfx { LevelUp, Next, Attach, Button, Over };
    int sfxCursor;

    [Header("---------------------[UI]")]
    public Text scoreText;
    public Text maxScoreText;
    public Text subScoreText;
    public GameObject endGroup;
    public GameObject startGroup;

    [Header("---------------------[ETC]")]
    public GameObject line;
    public GameObject bottom;

    public void GameStart()
    {
        line.SetActive(true);
        bottom.SetActive(true);
        scoreText.gameObject.SetActive(true);
        maxScoreText.gameObject.SetActive(true);
        startGroup.SetActive(false);
        SfxPlay(Sfx.Button);
        bgmPlayer.Play();
        Invoke("NextDongle", 1.5f);
    }

    void Awake()
    {
        Application.targetFrameRate = 60;
        donglePool = new List<Dongle>();
        effectPool = new List<ParticleSystem>();
        for (int index = 0; index < poolSize; index++)
        {
            MakeDongle();
        }
        if (!PlayerPrefs.HasKey("MaxScore"))
        {
            PlayerPrefs.SetInt("MaxScore",0);
        }
        maxScoreText.text = PlayerPrefs.GetInt("MaxScore").ToString(); //데이터 저장하는 pref를 사용
    }

    Dongle MakeDongle()
    {
        GameObject instantEffectObj = Instantiate(effectPrefab, effectGroup);
        instantEffectObj.name = "Effect" + effectPool.Count;
        ParticleSystem instantEffect = instantEffectObj.GetComponent<ParticleSystem>();
        effectPool.Add(instantEffect);

        GameObject instantDongleObj = Instantiate(donglePrefab, dongleGroup);
        instantDongleObj.name = "Dongle" + donglePool.Count;
        Dongle instantDongle = instantDongleObj.GetComponent<Dongle>();
        instantDongle.manager = this;
        instantDongle.effect = instantEffect;
        donglePool.Add(instantDongle);

        return instantDongle;
    }

    Dongle GetDongle()
    {
        for (int index = 0; index < donglePool.Count; index++)
        {
            poolCursor = (poolCursor + 1) % donglePool.Count;
            if (!donglePool[poolCursor].gameObject.activeSelf)
            {
                return donglePool[poolCursor];
            }
        }
        return MakeDongle();
    }

    void NextDongle()
    {
        if (isOver)
            return;
        lastDongle = GetDongle();
        lastDongle.level = Random.Range(0, maxLevel);
        lastDongle.gameObject.SetActive(true);
        SfxPlay(Sfx.Next);
        StartCoroutine("WaitNext"); //코루틴 실행
    }

    IEnumerator WaitNext() //코루틴
    {
        while (lastDongle != null)
        {
            yield return null; //안쓰면 while 자체를 계속 돌아 유니티 멈춤
        }

        yield return new WaitForSeconds(2.5f); //2.5초 기다리고 실행
        NextDongle();
    }

    public void TouchDown()
    {
        if (lastDongle == null)
            return;
        lastDongle.Drag();
    }

    public void TouchUp()
    {
        if (lastDongle == null)
            return;
        lastDongle.Drop();
        lastDongle = null;
    }

    public void GameOver()
    {
        if (isOver)
            return;
        isOver = true;
        StartCoroutine("GameOverRoutine");
    }
    IEnumerator GameOverRoutine()
    {
        Dongle[] dongles = FindObjectsOfType<Dongle>();

        for (int index = 0; index < dongles.Length; index++)
        {
            dongles[index].rigid.simulated = false;
        }

        for (int index = 0; index < dongles.Length; index++)
        {
            dongles[index].Hide(Vector3.up * 100);
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(1f);
        int maxScore = Mathf.Max(score, PlayerPrefs.GetInt("MaxScore"));
        PlayerPrefs.SetInt("MaxScore",maxScore);
        subScoreText.text = "점수 : " + score;
        endGroup.SetActive(true);
        bgmPlayer.Stop();
        SfxPlay(Sfx.Over);
    }
    public void Reset()
    {
        SfxPlay(Sfx.Button);
        StartCoroutine("ResetRoutine");
    }

    IEnumerator ResetRoutine()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(0);
    }

    public void SfxPlay(Sfx type)
    {
        switch (type)
        {
            case Sfx.LevelUp:
                sfxPlayer[sfxCursor].clip = sfxClip[Random.Range(0, 3)];
                break;
            case Sfx.Next:
                sfxPlayer[sfxCursor].clip = sfxClip[3];
                break;
            case Sfx.Attach:
                sfxPlayer[sfxCursor].clip = sfxClip[4];
                break;
            case Sfx.Button:
                sfxPlayer[sfxCursor].clip = sfxClip[5];
                break;
            case Sfx.Over:
                sfxPlayer[sfxCursor].clip = sfxClip[6];
                break;
        }
        sfxPlayer[sfxCursor].Play();
        sfxCursor = (sfxCursor + 1) % sfxPlayer.Length;
    }
    void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            Application.Quit();
        }
    }
    void LateUpdate()
    {
        scoreText.text = score.ToString();
    }
}

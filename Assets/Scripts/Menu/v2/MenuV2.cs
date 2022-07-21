using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class MenuV2 : MonoBehaviour
{
    public RectTransform mainScreen;

    public RectTransform playScreen;
    public RectTransform optionsScreen;
    public RectTransform menuButtonsRect;
    public Image inputBlocker;

    [Header("Audio")] public AudioSource musicSource;
    public AudioClip menuClip;
    private Stopwatch _menuStopwatch;
    public Animator heckerAnimator;
    private int _beatCounter;

    [Header("Background")] public Camera backgroundCamera;

    public SpriteRenderer backgroundSprite;

    public UIGradient backgroundGradient;

    [Header("Song List")] public RectTransform songListRect;

    public GameObject bundleButtonPrefab;

    public GameObject songButtonPrefab;

    public Sprite defaultCoverSprite;

    public bool canChangeSongs = true;
    public GameObject migrateBundlesButton;

    private Dictionary<BundleButtonV2, List<SongButtonV2>> bundles =
        new Dictionary<BundleButtonV2, List<SongButtonV2>>();

    [Header("Mode of Play")] public GameObject playModeScreen;

    [Header("Song Info")] public Image songCoverImage;
    public TMP_Text songNameText;
    public TMP_Text highScoreText;
    private int _lastScore = 0;
    [FormerlySerializedAs("songCharterText")] public TMP_Text songCreditsText;
    public TMP_Text songDescriptionText;
    public TMP_Dropdown songDifficultiesDropdown;
    public TMP_Dropdown songModeDropdown;
    public GameObject selectSongScreen;
    public GameObject songInfoScreen;
    public GameObject loadingSongScreen;

    [Header("Notifications")] public GameObject notificationObject;
    public RectTransform notificationLists;

    [Space] public Button[] menuButtons;
    public GameObject[] menuScreens;
    
    private SongMetaV2 _currentMeta;
    private string _songsFolder;
    
    public static MenuV2 Instance;
    public static int lastSelectedBundle;
    public static int lastSelectedSong;

    public static StartPhase startPhase;
    
    // Start is called before the first frame update
    void Start()
    {
        InitializeMenu();
    }

    public enum StartPhase
    {
        Nothing,
        SongList,
        Offset
    }

    public void ReloadSongList()
    {
        selectSongScreen.SetActive(true);
        songInfoScreen.SetActive(false);
        
        if (songListRect.childCount != 0)
        {
            foreach (RectTransform child in songListRect)
            {
                Destroy(child.gameObject);
            }
        }
        
        if (!Directory.Exists(_songsFolder))
        {
            Directory.CreateDirectory(_songsFolder);
        }

        

        SearchOption option = SearchOption.TopDirectoryOnly;

        List<string> allDirectories = new List<string>();
        allDirectories.AddRange(Directory.GetDirectories(_songsFolder, "*", option));
        
        allDirectories.AddRange(GameModLoader.bundleModDirectories.Keys);
        
        foreach (string dir in allDirectories)
        {
            if (File.Exists(dir + "/bundle-meta.json"))
            {
                BundleMeta bundleMeta =
                    JsonConvert.DeserializeObject<BundleMeta>(File.ReadAllText(dir + "/bundle-meta.json"));

                if (bundleMeta == null)
                {
                    Debug.LogError("Error whilst trying to read JSON file! " + dir + "/bundle-meta.json");
                    break;
                }

                BundleButtonV2 newWeek = Instantiate(bundleButtonPrefab, songListRect).GetComponent<BundleButtonV2>();

                newWeek.Creator = bundleMeta.authorName;
                newWeek.Name = bundleMeta.bundleName;
                newWeek.directory = dir;
                newWeek.isMod = GameModLoader.bundleModDirectories.Keys.Contains(dir);
                newWeek.SongButtons = new List<SongButtonV2>();
                print("Searching in " + dir);

                List<SongButtonV2> songButtons = new List<SongButtonV2>();

                foreach (string songDir in Directory.GetDirectories(dir, "*", option))
                {
                    print("We got " + songDir);
                    if (File.Exists(songDir + "/meta.json") & File.Exists(songDir + "/Inst.ogg"))
                    {
                        SongMetaV2 meta = JsonConvert.DeserializeObject<SongMetaV2>(File.ReadAllText(songDir + "/meta.json"));

                        if (meta == null)
                        {
                            Debug.LogError("Error whilst trying to read JSON file! " + songDir + "/meta.json");
                            break;
                        }

                        meta.bundleMeta = bundleMeta;
                        meta.isFromModPlatform = newWeek.isMod;
                        if (meta.isFromModPlatform)
                        {
                            meta.modURL = GameModLoader.bundleModDirectories[dir];
                        }
                        
                        SongButtonV2 newSong = Instantiate(songButtonPrefab,songListRect).GetComponent<SongButtonV2>();
                        
                        newSong.Meta = meta;
                        newSong.Meta.songPath = songDir;
                
                        string coverDir = songDir + "/Cover.png";
                
                        if (File.Exists(coverDir))
                        {
                            byte[] coverData = File.ReadAllBytes(coverDir);

                            Texture2D coverTexture2D = new Texture2D(512,512);
                            coverTexture2D.LoadImage(coverData);

                            newSong.CoverArtSprite = Sprite.Create(coverTexture2D,
                                new Rect(0, 0, coverTexture2D.width, coverTexture2D.height), new Vector2(0, 0), 100);
                            newSong.Meta.songCover = newSong.CoverArtSprite;

                        }
                        else
                        {
                            newSong.CoverArtSprite = defaultCoverSprite;
                            newSong.Meta.songCover = defaultCoverSprite;
                        }

                        newWeek.SongButtons.Add(newSong);

                        newSong.gameObject.SetActive(false);
                        
                        

                        newSong.GetComponent<Button>().onClick.AddListener(() =>
                        {
                            ChangeSong(newSong.Meta);

                            lastSelectedBundle = GetBundleIndex(newWeek);
                            lastSelectedSong = bundles[newWeek].IndexOf(newSong);
                        });

                        songButtons.Add(newSong);
                    }
                    else
                    {
                        Debug.LogError("Failed to find required files in " + songDir);
                    }
                }

                newWeek.UpdateCount();
                bundles.Add(newWeek, songButtons);
            }
            
            
        }

        if (startPhase == StartPhase.SongList)
        {
            startPhase = StartPhase.Nothing;

            LoadingTransition.instance.Hide();

            BundleButtonV2 bundleButton = bundles.Keys.ElementAt(lastSelectedBundle);
            bundleButton.ToggleSongsVisibility();

            musicSource.volume = OptionsV2.menuVolume;
            
            ChangeSong(bundles[bundleButton][lastSelectedSong].Meta);
            
        }
    }

    public void UpdateScoreText()
    {
        
        
        string highScoreSave = _currentMeta.songName + _currentMeta.bundleMeta.bundleName +
                               songDifficultiesDropdown.options[songDifficultiesDropdown.value].text.ToLower() +
                               (songModeDropdown.value + 1);
        int highScore = PlayerPrefs.GetInt(highScoreSave, 0);
        print("High Score for " + highScoreSave + " is " + highScore);
        if (songModeDropdown.value != 3)
        {
            LeanTween.value(_lastScore, highScore, .35f).setOnUpdate(value =>
            {
                highScoreText.text = $"High Score: <color=white>{(int)value}</color>";
            }).setOnComplete(() =>
            {
                _lastScore = highScore;
            });
        }
        else
        {
            highScoreText.text = "High Score not available for AutoPlay.";
        }

        
    }

    public int GetBundleIndex(BundleButtonV2 item)
    {

        for (int i = 0; i < bundles.Keys.Count; i++)
        {
            if (bundles.Keys.ElementAt(i) == item)
            {
                return i;
            }
        }

        return 0;
    }
    
    public void ChangeSong(SongMetaV2 meta)
    {
        print("Checking if we can change songs. It is " + canChangeSongs);
        if (!canChangeSongs) return;
        print("Updating info");
        songNameText.text = meta.songName;
        songDescriptionText.text = "<color=yellow>Description:</color> " + meta.songDescription;
        songCoverImage.sprite = meta.songCover;

        songCreditsText.text = string.Empty;
        
        foreach (string role in meta.credits.Keys.ToList())
        {
            string memberName = meta.credits[role];

            songCreditsText.text += $"<color=yellow>{role}:</color> {memberName}\n";
        }
        
        songDifficultiesDropdown.ClearOptions();

        songDifficultiesDropdown.AddOptions(meta.difficulties.Keys.ToList());
        
        loadingSongScreen.SetActive(true);

        selectSongScreen.SetActive(false);
        songInfoScreen.SetActive(false);

        LeanTween.value(musicSource.gameObject, musicSource.volume, 0, 1f).setOnComplete(() =>
        {
            StartCoroutine(nameof(LoadSongAudio), meta.songPath+"/Inst.ogg");
        }).setOnUpdate(value =>
        {
            musicSource.volume = value;
        });
        

        _currentMeta = meta;
    }

    public void PlaySong()
    {
        var difficultiesList = _currentMeta.difficulties.Keys.ToList();
        Song.difficulty = difficultiesList[songDifficultiesDropdown.value];
        Song.modeOfPlay = songModeDropdown.value + 1;
        Song.currentSongMeta = _currentMeta;

        LoadingTransition.instance.Show(() => SceneManager.LoadScene("Game_Backup3"));
    }
    

    IEnumerator LoadSongAudio(string path)
    {
        WWW www = new WWW(path);
        if (www.error != null)
        {
            Debug.LogError(www.error);
        }
        else
        {
            canChangeSongs = false;
            musicSource.clip = www.GetAudioClip();
            while (musicSource.clip.loadState != AudioDataLoadState.Loaded)
                yield return new WaitForSeconds(0.1f);
            musicSource.Play();
            LeanTween.value(musicSource.gameObject, musicSource.volume, OptionsV2.instVolume, 1f).setOnUpdate(value =>
            {
                musicSource.volume = value;
            });
            canChangeSongs = true;
            loadingSongScreen.SetActive(false);
            songInfoScreen.SetActive(true);
            UpdateScoreText();
        }
    }
    
    //FREEPLAY
    public void LaunchSongScreen(WeekSong song)
    {
        playModeScreen.SetActive(true);

        Song.weekMode = false;
        Song.currentSong = song;
    }
    public void LaunchSongScreen(Week week)
    {
        playModeScreen.SetActive(true);

        Song.weekMode = true;
        Song.currentWeek = week;
        Song.currentWeekIndex = 0;
    }

    public void BeginSong(int modeOfPlay)
    {
        Song.modeOfPlay = modeOfPlay;
        
        if (Song.weekMode)
        {
            LoadingTransition.instance.Show(() =>
            {
                VideoPlayerScene.nextScene = "Game_Backup3";
                VideoPlayerScene.videoToPlay = Song.currentWeek.songs[Song.currentWeekIndex].cutscene;
                SceneManager.LoadScene("Video");
            });
        }
        else
        {
            LoadingTransition.instance.Show(() =>
            {
                SceneManager.LoadScene("Game_Backup3");
            });
        }
    }

    
    public void InitializeMenu()
    {
        Instance = this;

        LeanTween.reset();

        LeanTween.init(99999);

        _menuStopwatch = new Stopwatch();
        
        /*switch (startPhase)
        {
            case StartPhase.Nothing:
                
                break;
            case StartPhase.SongList:
                canChangeSongs = true;

                mainScreen.gameObject.SetActive(false);
                playScreen.gameObject.SetActive(true);

                playScreen.LeanMoveY(0, 0f);

                ReloadSongList();
                break;
            
            case StartPhase.Offset:
                mainScreen.gameObject.SetActive(false);
                optionsScreen.gameObject.SetActive(true);

                optionsScreen.LeanMoveY(0, 0f).setOnComplete(() =>
                {
                    OptionsV2.instance.LoadNotePrefs();
                    LoadingTransition.instance.Hide();
                });
                break;
        }*/
        musicSource.clip = menuClip;
        musicSource.volume = OptionsV2.menuVolume;
        musicSource.Play();
        _menuStopwatch.Start();

        DiscordController.instance.SetMenuState("Idle");

        LoadingTransition.instance.Hide();
    }

    public void OptionsScreenTransition(bool toOptions)
    {
        if (toOptions)
        {
            DiscordController.instance.SetMenuState("Editing Options");
            mainScreen.gameObject.SetActive(false);
            optionsScreen.gameObject.SetActive(true);
            menuButtonsRect.gameObject.SetActive(false);
        }
        else
        {
            DiscordController.instance.SetMenuState("Idle");
            mainScreen.gameObject.SetActive(true);
            optionsScreen.gameObject.SetActive(false);
            menuButtonsRect.gameObject.SetActive(true);
            
            foreach(Button btn in menuButtons)
            {
                btn.interactable = true;
            }
        }
    }
    
    public void OpenPlayScreenFromMenu()
    {
        TransitionScreen(mainScreen, playScreen, () => DiscordController.instance.SetMenuState("Selecting a Song"));
        canChangeSongs = true;
    }

    public void OpenMenuFromPlayScreen()
    {
        if (!canChangeSongs) return;
        TransitionScreen(playScreen, mainScreen, () => DiscordController.instance.SetMenuState("Idle"));
        if (musicSource.clip != menuClip)
        {
            musicSource.Stop();
            musicSource.clip = menuClip;
            musicSource.volume = OptionsV2.menuVolume;
            musicSource.Play();
        }
    }

    public void ChangeSelectedButton(Button newSelectedButton)
    {
        foreach(Button btn in menuButtons)
        {
            btn.interactable = true;
        }

        newSelectedButton.interactable = false;
    }

    public void ChangeSelectedScreen(GameObject newScreen)
    {
        foreach(GameObject menuScreen in menuScreens)
        {
            menuScreen.SetActive(false);
        }

        newScreen.SetActive(true);
    }

    public void OpenURL(string link)
    {
        Application.OpenURL(link);
    }

    public void TransitionScreen(RectTransform oldScreen, RectTransform newScreen, Action onComplete = null)
    {
        inputBlocker.enabled = true;
        oldScreen.LeanMoveY(-720,1f).setEaseOutExpo().setOnComplete(() =>
        {
            oldScreen.gameObject.SetActive(false);
            newScreen.gameObject.SetActive(true);
            newScreen.LeanMoveY(-720, 0);
            newScreen.LeanMoveY(0, 1f).setEaseOutExpo().setOnComplete(() =>
            {
                inputBlocker.enabled = false;
                onComplete?.Invoke();
            });

        });
    }
    
    

    public void DisplayNotification(Color color, string text)
    {
        GameObject notification = Instantiate(notificationObject, notificationLists);
        NotificationObject notificationScript = notification.GetComponent<NotificationObject>();

        notificationScript.notificationText.text = text;
        notificationScript.BackgroundColor = color;

    }
    
    // Update is called once per frame
    void Update()
    {
        if (_menuStopwatch.IsRunning)
        {
            if (_menuStopwatch.ElapsedMilliseconds / 1000f >= 60f / 104f)
            {
                _menuStopwatch.Restart();

                _beatCounter++;
                
                if(_beatCounter % 2 == 0)
                {

                    heckerAnimator.Play("Dancin Hecker", 0, 0);
                    heckerAnimator.speed = 0;

                    heckerAnimator.Play("Dancin Hecker");
                    heckerAnimator.speed = 1;
                }
            }
        }
    }
}

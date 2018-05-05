using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;
using KMHelper;

public class Synonyms : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombModule Module;
    public KMBombInfo Info;
    public KMSelectable[] Buttons;
    public TextMesh Screen, NumberText;
    public TextMesh NegetiveButton, BadLabel;
    public TextMesh GoodButton, GoodLabel;

    private static int _moduleIDCounter = 1;
    private int _moduleID;
    private bool Active;
    private bool _isSolved;
    private string numbers = "0123456789";
    private string goodWords = ("OK","OKAY","CONFIRM","ENTER","EXECUTE","VERIFY","SEND","APPROVE","SUBMIT","SELECT","YES");
    private string badWords = "CANCELANNULERASEDELETESTOPOPPOSEDISCARDREJECTDECLINEREFUSENO";

    private int[,] original = new int[11, 11]{
        {1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0},
        {0, 0, 9, 8, 7, 6, 5, 4, 3, 2, 1},
        {2, 3, 5, 7, 9, 1, 4, 6, 8, 0, 4}, 
        {4, 2, 1, 8, 6, 5, 9, 3, 0, 7, 2},
        {5, 1, 2, 4, 9, 0, 6, 9, 3, 8, 7},
        {8, 4, 2, 1, 9, 3, 1, 6, 5, 7, 0},
        {6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 0},
        {5, 6, 7, 5, 1, 3, 9, 0, 2, 4, 8},
        {7, 1, 2, 3, 7, 5, 6, 4, 8, 9, 0},
        {3, 7, 0, 2, 8, 0, 1, 4, 6, 9, 5},
        {1, 3, 2, 4, 7, 5, 6, 0, 8, 9, 3}
    };

    private int[,] columnDouble = new int[11, 11]{
        {1, 2, 3, 4, 5, 6, 4, 8, 9, 0, 0},
        {0, 0, 9, 8, 7, 6, 0, 4, 3, 2, 1},
        {2, 3, 5, 7, 9, 1, 8, 6, 8, 0, 4},
        {4, 2, 1, 8, 6, 5, 8, 3, 0, 7, 2},
        {5, 1, 2, 4, 9, 0, 2, 9, 3, 8, 7},
        {8, 4, 2, 1, 9, 3, 2, 6, 5, 7, 0},
        {6, 7, 8, 9, 0, 1, 4, 3, 4, 5, 0},
        {5, 6, 7, 5, 1, 3, 8, 0, 2, 4, 8},
        {7, 1, 2, 3, 7, 5, 2, 4, 8, 9, 0},
        {3, 7, 0, 2, 8, 0, 2, 4, 6, 9, 5},
        {1, 3, 2, 4, 7, 5, 2, 0, 8, 9, 3}
    };

    private int[,] rowSwap = new int[11, 11]{
        {1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0},
        {6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 0},
        {2, 3, 5, 7, 9, 1, 4, 6, 8, 0, 4},
        {4, 2, 1, 8, 6, 5, 9, 3, 0, 7, 2},
        {5, 1, 2, 4, 9, 0, 6, 9, 3, 8, 7},
        {8, 4, 2, 1, 9, 3, 1, 6, 5, 7, 0},
        {0, 0, 9, 8, 7, 6, 5, 4, 3, 2, 1},
        {5, 6, 7, 5, 1, 3, 9, 0, 2, 4, 8},
        {7, 1, 2, 3, 7, 5, 6, 4, 8, 9, 0},
        {3, 7, 0, 2, 8, 0, 1, 4, 6, 9, 5},
        {1, 3, 2, 4, 7, 5, 6, 0, 8, 9, 3}
    };

    private int[,] unicorn = new int[11, 11]{
        {1, 2, 3, 4, 5, 6, 4, 8, 9, 0, 0},
        {6, 7, 8, 9, 0, 1, 4, 3, 4, 5, 0},
        {2, 3, 5, 7, 9, 1, 8, 6, 8, 0, 4},
        {4, 2, 1, 8, 6, 5, 8, 3, 0, 7, 2},
        {5, 1, 2, 4, 9, 0, 2, 9, 3, 8, 7},
        {8, 4, 2, 1, 9, 3, 2, 6, 5, 7, 0},
        {0, 0, 9, 8, 7, 6, 0, 4, 3, 2, 1},
        {5, 6, 7, 5, 1, 3, 8, 0, 2, 4, 8},
        {7, 1, 2, 3, 7, 5, 2, 4, 8, 9, 0},
        {3, 7, 0, 2, 8, 0, 2, 4, 6, 9, 5},
        {1, 3, 2, 4, 7, 5, 2, 0, 8, 9, 3}
    };

    // Loading Screen
    void Start()
    {
        _moduleID = _moduleIDCounter++;
        Module.OnActivate += delegate () { Active = true; };
    }
    //Room shown, lights off
    private void Awake()
    {
        for (int i = 0; i < 4; i++)
        {
            //Button handling 
            int b = i;
            Buttons[i].OnInteract += delegate ()
            {
                Buttons[b].AddInteractionPunch();
                Answer(b);
                return false;
            };
        }
        //Logging
        {
            //Logging goes here
        }
    }

    void Solved()
    {
        Module.HandlePass();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
        _isSolved = true;
        Debug.LogFormat("[Synonyms #{0}] The module has been solved!", _moduleID);
    }

    void Strike(string message)
    {
        Module.HandleStrike();
        Debug.LogFormat("[Synonyms #{0}] {1}", _moduleID, message);
    }

    // The module answer
    void Answer(int i)
    {
        //Thing that chooses number
        //Now the thing that decides which table to use
        //if both applt
        if (BombInfo.GetPortPlates().Count(x => x.Length == 0) == 2 && Info.IsIndicatorOn(Indicator.IND))
        {
            //use table unicorn
        }
        else if (BombInfo.GetPortPlates().Count(x => x.Length == 0) == 2)
        {
            //use table columnDouble
        }
        else if (Info.IsIndicatorOn(Indicator.IND))
        {
            //use table rowSwap
        }
        else
        {
            //use table original
        }
    }
}

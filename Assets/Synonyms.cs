using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;
using KMHelper;

public static class GeneralExtensions
{
    public static bool EqualsAny(this object obj, params object[] targets)
    {
        return targets.Contains(obj);
    }
}

public class Synonyms : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombModule Module;
    public KMBombInfo Info;
    public KMSelectable[] Buttons;
    public TextMesh NumberText;
    public TextMesh BadLabel;
    public TextMesh GoodLabel;

    private static int _moduleIDCounter = 1;
    private int lastDigit;
    private int _moduleID;
    private int current;
    private bool Active;
    private bool _isSolved;
    private bool ind;
    private bool EmptyPorts;
    private bool match;
    private bool rule;
    private string[] goodWords = { "OK", "OKAY", "CONFIRM", "ENTER", "EXECUTE", "VERIFY", "SEND", "APPROVE", "SUBMIT", "SELECT", "YES" };
    private string[] badWords = { "CANCEL", "ANNUL", "ERASE", "DELETE", "STOP", "OPPOSE", "DISCARD", "REJECT", "DECLINE", "REFUSE", "NO" };
    private List<string> goodButton = new List<string>();
    private List<string> badButton = new List<string>();
    private List<int> list1 = new List<int>();
    private List<int> list2 = new List<int>();

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

    // Loading Screen
    void Start()
    {
        _moduleID = _moduleIDCounter++;
        Module.OnActivate += delegate () { Active = true; };
        lastDigit = Info.GetSerialNumberNumbers().Last();
        ind = Info.IsIndicatorOn(Indicator.IND);
        EmptyPorts = Info.GetPortPlates().Where(x => x.Length == 0).Count() == 2;
        var unicorn = UnityEngine.Random.Range(0, 100);
        var screen = UnityEngine.Random.Range(0, 10);
        NumberText.text = screen.ToString();
        bool temp = false;
        rule = false;
        while (!temp) temp = WordRandomization(unicorn, screen);

        BadLabel.text = badButton[current];
        GoodLabel.text = goodButton[current];
    }

    bool WordRandomization(int unicorn, int screen)
    {
        match = true;
        list1 = new List<int>();
        list2 = new List<int>();
        goodButton = new List<string>();
        badButton = new List<string>();

        foreach (string word in badWords)
        {
            var i = UnityEngine.Random.Range(0, goodWords.Count());
            while (list1.Contains(i)) i = UnityEngine.Random.Range(0, badWords.Count());
            list1.Add(i);
            badButton.Add(badWords[i]);
        }
        foreach (string word in goodWords)
        {
            var i = UnityEngine.Random.Range(0, goodWords.Count());
            while (list2.Contains(i)) i = UnityEngine.Random.Range(0, goodWords.Count());
            list2.Add(i);
            goodButton.Add(goodWords[i]);
        }
        if (unicorn < 3 || unicorn > 97)
        {
            screen = 3;
            match = false;
        }

        if (!CheckAnswer(screen)) return false;
        return true;
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
    }

    void Solved()
    {
        Module.HandlePass();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
        _isSolved = true;
        NumberText.color = Color.clear;
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

        var correct = 0;

        for (int j = 0; j < goodButton.Count; j++)
        {
            if (original[list1[j], list2[j]].Equals(int.Parse(NumberText.text))) correct = j;
        }

        switch (i)
        {
            case 0:
                if (GoodLabel.text.Equals("EXECUTE") && !match) Solved();
                else if (!match) Strike(string.Format("No matches, press the button labeled \"EXECUTE\". Selected: {0}, Expected: EXECUTE", goodButton[current]));
                else if ((int.Parse(NumberText.text) % 2 == 0) && current == correct) Solved();
                else if ((int.Parse(NumberText.text) % 2 == 0)) Strike(string.Format("Selected: {0}, Expected: {1}", goodButton[current], goodButton[correct]));
                else Strike(string.Format("Selected: {0}, Expected: {1}", goodButton[current], badButton[correct]));
                break;
            case 1:
                if (!match) Strike(string.Format("No matches, press the button labeled \"EXECUTE\". Selected: {0}, Expected: EXECUTE", badButton[current]));
                else if ((int.Parse(NumberText.text) % 2 == 1) && current == correct) Solved();
                else if ((int.Parse(NumberText.text) % 2 == 1)) Strike(string.Format("Selected: {0}, Expected: {1}", badButton[current], badButton[correct]));
                else Strike(string.Format("Selected: {0}, Expected: {1}", badButton[current], goodButton[correct]));
                break;
            case 2:
                if (_isSolved) break;
                current++;
                if (current > (goodButton.Count - 1)) current = 0;
                GoodLabel.text = goodButton[current];
                BadLabel.text = badButton[current];
                break;
            case 3:
                if (_isSolved) break;
                current--;
                if (current < 0) current = goodButton.Count - 1;
                GoodLabel.text = goodButton[current];
                BadLabel.text = badButton[current];
                break;
        }
    }

    private bool CheckAnswer(int screen)
    {
        if (ind && lastDigit.Equals(5) && !rule)
        {
            for (int y = 0; y < original.GetLength(0); y++)
            {
                var hold = original[1, y];
                original[1, y] = original[6, y];
                original[6, y] = hold;
            }
        }
        if (EmptyPorts && !rule)
        {
            for (int x = 0; x < original.GetLength(1); x++)
            {
                original[x, 6] *= 2;
                if (original[x, 6] > 9) original[x, 6] -= 10;
            }
        }
        if (!rule) rule = true;
        var matchCount = 0;

        for (int i = 0; i < goodButton.Count; i++)
        {
            var count = original[list1[i], list2[i]];
            if (count == screen) matchCount++;
        }
        if (match && matchCount == 1) return true;
        else if (!match && matchCount == 0) return true;
        else return false;
    }

    private string TwitchHelpMessage = "Cycle the words using !{0} cycle. Move up and down individually using !{0} up/down. Submit 'okay' or 'cancel' by using !{0} [word]";

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        if (command.Equals("cycle"))
        {
            yield return null;
            for (int i = 0; i < goodButton.Count; i++)
            {
                Buttons[2].OnInteract();
                yield return new WaitForSeconds(2);
            }
        }
        else if (command.EqualsAny("press up", "up"))
        {
            yield return null;
            yield return Buttons[2].OnInteract();
        }
        else if (command.EqualsAny("press down", "down"))
        {
            yield return null;
            yield return Buttons[3].OnInteract();
        }
        else if (command.StartsWith("submit"))
        {
            var split = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length > 2) yield break;
            var list = new List<string>();
            var button = Buttons[0];
            if (split[1].ToUpperInvariant().EqualsAny(goodWords))
            {
                list = goodButton;
                button = Buttons[0];
            }
            else if (split[1].ToUpperInvariant().EqualsAny(badWords))
            {
                list = badButton;
                button = Buttons[1];
            }
            else yield break;
            var index = list.IndexOf(split[1].ToUpperInvariant());
            Debug.LogFormat(index.ToString() + " " + list[0]);
            if (index < 0) yield break;
            while (!index.Equals(current))
            {
                yield return null;
                yield return Buttons[3].OnInteract();
                yield return null;
            }
            yield return button.OnInteract();
        }
    }
}

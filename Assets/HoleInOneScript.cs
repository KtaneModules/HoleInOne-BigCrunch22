using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class HoleInOneScript : MonoBehaviour
{
	public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;
	public KMRuleSeedable RuleSeed;
	
	public AudioClip[] SFX;
	public KMSelectable[] ArrowsAndCircle;
	public GameObject[] Sticks, BallColors;
	public GameObject Everything1, Everything2, BallHold;
	public TextMesh[] TextAndShadow;
	
	string[][] NumberTable = new string[][]{
        new string[] {"V", "W", "F", "R", "T"},
        new string[] {"E", "U", "X", "D", "I"},
        new string[] {"O", "M", "P", "B", "Q"},
        new string[] {"J", "H", "N", "L", "Z"},
        new string[] {"C", "G", "Y", "A", "S"}
    };
	
	int[][] RuleNumbers = {
		new int[] {4, 2, 3, 12, 1, 8},
		new int[] {6, 3, 1, 4, 2, 9},
		new int[] {7, 1, 6, 8, 13, 5},
		new int[] {4, 2, 3, 12, 1, 8},
		new int[] {12, 10, 18, 5, 7, 3}
	};
	
	int StickNum = 0, GatheredNumber = 0, SolveCount = -1, BallValue = 0, HoldValue = 0, PowerNeeded = 0, StoredValue = 0;
	int rValue = 0,  gValue = 0, bValue = 0;
	bool Clicked = false, IncorrectAnswer = false, Pressable = false, AbleToHillBall = false, Checking = false;
	string Log = "";
	string[] Alphabreak = {"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"};
	Coroutine Hold;

	//Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool ModuleSolved;
	
	void Awake()
    {
		moduleId = moduleIdCounter++;
		for (int i = 0; i < ArrowsAndCircle.Length; i++)
		{
			int Press = i;
			ArrowsAndCircle[i].OnInteract += delegate ()
			{
				StartCoroutine(PressButton(Press));
				return false;
			};
		}
		
		for (int x = 0; x < BallColors.Length; x++)
		{
			int Press = x;
			BallColors[x].GetComponent<KMSelectable>().OnInteract += delegate()
			{
				BallPress(Press);
				return false;
			};
		}
		
		BallHold.GetComponent<KMSelectable>().OnInteract += delegate() { Hold = StartCoroutine(HoldBallCoroutine()); return false; };
		BallHold.GetComponent<KMSelectable>().OnInteractEnded += delegate() { StopCoroutine(Hold); StartCoroutine(Toss());};
	}
	
	void Start()
	{
		var RuleSeedRNG = RuleSeed.GetRNG();
		Debug.LogFormat("[Hole in One #{0}] Ruleseed Number: {1}", moduleId, RuleSeedRNG.Seed.ToString());
		if (RuleSeedRNG.Seed != 1)
		{
			string[] ShuffledLetters = {"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"};
			RuleSeedRNG.ShuffleFisherYates(ShuffledLetters);
			for (int x = 0; x < 25; x++)
			{
				NumberTable[x/5][x%5] = ShuffledLetters[x];
			}
			for (int x = 0; x < RuleNumbers.Length; x++)
			{
				for (int y = 0; y < RuleNumbers[x].Length; y++)
				{
					RuleNumbers[x][y] = RuleSeedRNG.Next(1,21);
				}
			}
		}
		Log = "Golf ball colors: ";
		StickNum = UnityEngine.Random.Range(0,5);
		Sticks.Shuffle();
		for (int x = 0; x < Sticks.Length; x++)
		{
			if (x != StickNum)
			{
				Sticks[x].SetActive(false);
			}
			
			else
			{
				Sticks[x].SetActive(true);
			}
			switch (UnityEngine.Random.Range(0,8))
			{
				case 0:
					BallColors[x].GetComponent<MeshRenderer>().material.color = new Color (1f, 0f, 0f);
					Log += x != Sticks.Length - 1 ? "Red" + ", " : "Red";
					break;
				case 1:
					BallColors[x].GetComponent<MeshRenderer>().material.color = new Color (0f, 1f, 0f);
					Log += x != Sticks.Length - 1 ? "Green" + ", " : "Green";
					break;
				case 2:
					BallColors[x].GetComponent<MeshRenderer>().material.color = new Color (0f, 0f, 1f);
					Log += x != Sticks.Length - 1 ? "Blue" + ", " : "Blue";
					break;
				case 3:
					BallColors[x].GetComponent<MeshRenderer>().material.color = new Color (1f, 1f, 0f);
					Log += x != Sticks.Length - 1 ? "Yellow" + ", " : "Yellow";
					break;
				case 4:
					BallColors[x].GetComponent<MeshRenderer>().material.color = new Color (1f, 0f, 1f);
					Log += x != Sticks.Length - 1 ? "Magenta" + ", " : "Magenta";
					break;
				case 5:
					BallColors[x].GetComponent<MeshRenderer>().material.color = new Color (0f, 1f, 1f);
					Log += x != Sticks.Length - 1 ? "Cyan" + ", " : "Cyan";
					break;
				case 6:
					BallColors[x].GetComponent<MeshRenderer>().material.color = new Color (1f, 1f, 1f);
					Log += x != Sticks.Length - 1 ? "White" + ", " : "White";
					break;
				case 7:
					BallColors[x].GetComponent<MeshRenderer>().material.color = new Color (0f, 0f, 0f);
					Log += x != Sticks.Length - 1 ? "Black" + ", " : "Black";
					break;
				default:
					break;
			}
			BallColors[x].SetActive(false);
		}
		Everything2.SetActive(false);
		Everything1.SetActive(false);
		Module.OnActivate += Proceed;
	}
	
	void Update()
	{
		if (SolveCount != -1 && SolveCount !=  Bomb.GetSolvedModuleNames().Count())
		{
			SolveCount = Bomb.GetSolvedModuleNames().Count();
			RulesApplied();
		}
	}
	
	void Proceed()
	{
		SolveCount = Bomb.GetSolvedModuleNames().Count();
		Everything1.SetActive(true);
		RulesApplied();
		GetBallValue();
	}	
	
	void BallPress(int Press)
	{
		BallColors[Press].GetComponent<KMSelectable>().AddInteractionPunch(0.2f);
		if (Pressable)
		{
			Debug.LogFormat("[Hole in One #{0}] You used Golf Ball #{1}", moduleId, (Press + 1).ToString());
			Audio.PlaySoundAtTransform(SFX[4].name, transform);
			if (Press != BallValue)
			{
				IncorrectAnswer = true;
			}
			BallHold.GetComponent<MeshRenderer>().material.color = BallColors[Press].GetComponent<MeshRenderer>().material.color;
			Everything1.SetActive(false);
			Everything2.SetActive(true);
			if (Bomb.GetSerialNumberNumbers().Last() % 2 == 0)
			{
				Debug.LogFormat("[Hole in One #{0}] The last digit of the serial is even. You must perform the Caesar shift forwards.", moduleId);
				Debug.LogFormat("[Hole in One #{0}] The focused letter is {1}", moduleId, NumberTable[StoredValue % 5][BallValue]);
				PowerNeeded = (Array.IndexOf(Alphabreak, NumberTable[StoredValue % 5][BallValue]) + StoredValue) % 26;
				Debug.LogFormat("[Hole in One #{0}] After performing the Caesar shift, the current letter must be the letter \"{1}\"", moduleId, Alphabreak[PowerNeeded]);
				PowerNeeded = 25 - PowerNeeded;
				Debug.LogFormat("[Hole in One #{0}] After performing the Atbash cipher, the correct letter must be \"{1}\"", moduleId, Alphabreak[PowerNeeded]);
				Debug.LogFormat("[Hole in One #{0}] You must hit the ball when the power level is {1}", moduleId, (PowerNeeded+1).ToString());
			}
			
			else
			{
				Debug.LogFormat("[Hole in One #{0}] The last digit of the serial is odd. You must perform the Caesar shift backwards.", moduleId);
				Debug.LogFormat("[Hole in One #{0}] The focused letter is {1}", moduleId, NumberTable[StoredValue % 5][BallValue]);
				PowerNeeded = Array.IndexOf(Alphabreak, NumberTable[StoredValue % 5][BallValue]) - StoredValue;
				while (PowerNeeded < 0)
				{
					PowerNeeded += 26;
				}
				Debug.LogFormat("[Hole in One #{0}] After performing the Caesar shift, the current letter must be the letter \"{1}\"", moduleId, Alphabreak[PowerNeeded]);
				PowerNeeded = 25 - PowerNeeded;
				Debug.LogFormat("[Hole in One #{0}] After performing the Atbash cipher, the correct letter must be \"{1}\"", moduleId, Alphabreak[PowerNeeded]);
				Debug.LogFormat("[Hole in One #{0}] You must hit the ball when the power level is {1}", moduleId, (PowerNeeded+1).ToString());
			}
			Pressable = false;
			AbleToHillBall = true;
		}
	}
	
	IEnumerator PressButton(int Press)
	{
		ArrowsAndCircle[Press].AddInteractionPunch(.2f);
		if (Press == 0)
		{
			if (StickNum != 0 && !Clicked)
			{
				Sticks[StickNum].SetActive(false);
				StickNum--;
				Sticks[StickNum].SetActive(true);
			}
		}
		
		else if (Press == 1)
		{
			if (!Clicked)
			{
				Debug.LogFormat("[Hole in One #{0}] You pressed on the circle when the module had {1} solves.", moduleId, Bomb.GetSolvedModuleNames().Count().ToString());
				Debug.LogFormat("[Hole in One #{0}] The correct total based on the current solve: {1}", moduleId, GatheredNumber.ToString());
				Debug.LogFormat("[Hole in One #{0}] You must use Golf Stick #{1}", moduleId, (GatheredNumber % 5 + 1).ToString());
				Debug.LogFormat("[Hole in One #{0}] You used Golf Stick #{1}", moduleId, (StickNum + 1).ToString());
				StoredValue = GatheredNumber;
				if (StickNum != StoredValue % 5)
				{
					IncorrectAnswer = true;
				}
				Clicked = true;
				for (int x = 0; x < BallColors.Length; x++)
				{
					Audio.PlaySoundAtTransform(SFX[4].name, transform);
					BallColors[x].SetActive(true);
					yield return new WaitForSecondsRealtime(.2f);
				}
				Pressable = true;
				Debug.LogFormat("[Hole in One #{0}] {1}", moduleId, Log);
				Debug.LogFormat("[Hole in One #{0}] Values for each RGB components: R - {1}, G - {2}, B - {3}", moduleId, rValue.ToString(), gValue.ToString(), bValue.ToString());
				Debug.LogFormat("[Hole in One #{0}] If multiplied properly, the answer should be: {1}", moduleId, (1 * (rValue != 0 ? rValue : 1) * (gValue != 0 ? gValue : 1) * (bValue != 0 ? bValue : 1)).ToString());
				Debug.LogFormat("[Hole in One #{0}] You must use Golf Ball #{1}", moduleId, (BallValue + 1).ToString());
				
			}
		}
		
		else if (Press == 2 && !Clicked)
		{
			if (StickNum != Sticks.Length - 1)
			{
				Sticks[StickNum].SetActive(false);
				StickNum++;
				Sticks[StickNum].SetActive(true);
			}
		}
	}
	
	void RulesApplied()
	{
		bool NoneApplied = true;
		GatheredNumber = 0;
		if (Bomb.GetOnIndicators().Count() > Bomb.GetOffIndicators().Count())
		{
			NoneApplied = false;
			switch (Bomb.GetSolvedModuleNames().Count())
			{
				case 0: case 1: case 2: case 3:
					GatheredNumber += RuleNumbers[0][0];
					break;
				case 4: case 5: case 6:
					GatheredNumber += RuleNumbers[0][1];
					break;
				case 7: case 8: case 9:
					GatheredNumber += RuleNumbers[0][2];
					break;
				case 10: case 11: case 12:
					GatheredNumber += RuleNumbers[0][3];
					break;
				case 13: case 14: case 15:
					GatheredNumber += RuleNumbers[0][4];
					break;
				default:
					GatheredNumber += RuleNumbers[0][5];
					break;
			}
		}
		
		if (Bomb.GetPortCount() > Bomb.GetBatteryCount())
		{
			NoneApplied = false;
			switch (Bomb.GetSolvedModuleNames().Count())
			{
				case 0: case 1: case 2: case 3:
					GatheredNumber += RuleNumbers[1][0];
					break;
				case 4: case 5: case 6:
					GatheredNumber += RuleNumbers[1][1];
					break;
				case 7: case 8: case 9:
					GatheredNumber += RuleNumbers[1][2];
					break;
				case 10: case 11: case 12:
					GatheredNumber += RuleNumbers[1][3];
					break;
				case 13: case 14: case 15:
					GatheredNumber += RuleNumbers[1][4];
					break;
				default:
					GatheredNumber += RuleNumbers[1][5];
					break;
			}
		}
		
		if (new[] {char.Parse((Bomb.GetSolvedModuleNames().Count() % 10).ToString())}.Any(c => Bomb.GetSerialNumber().Contains(c)))
		{
			NoneApplied = false;
			switch (Bomb.GetSolvedModuleNames().Count())
			{
				case 0: case 1: case 2: case 3:
					GatheredNumber += RuleNumbers[2][0];
					break;
				case 4: case 5: case 6:
					GatheredNumber += RuleNumbers[2][1];
					break;
				case 7: case 8: case 9:
					GatheredNumber += RuleNumbers[2][2];
					break;
				case 10: case 11: case 12:
					GatheredNumber += RuleNumbers[2][3];
					break;
				case 13: case 14: case 15:
					GatheredNumber += RuleNumbers[2][4];
					break;
				default:
					GatheredNumber += RuleNumbers[2][5];
					break;
			}
		}
		
		if (new[] {'A', 'E', 'I', 'O', 'U'}.Any(c => Bomb.GetSerialNumber().Contains(c)))
		{
			NoneApplied = false;
			switch (Bomb.GetSolvedModuleNames().Count())
			{
				case 0: case 1: case 2: case 3:
					GatheredNumber += RuleNumbers[3][0];
					break;
				case 4: case 5: case 6:
					GatheredNumber += RuleNumbers[3][1];
					break;
				case 7: case 8: case 9:
					GatheredNumber += RuleNumbers[3][2];
					break;
				case 10: case 11: case 12:
					GatheredNumber += RuleNumbers[3][3];
					break;
				case 13: case 14: case 15:
					GatheredNumber += RuleNumbers[3][4];
					break;
				default:
					GatheredNumber += RuleNumbers[3][5];
					break;
			}
		}
		
		if (NoneApplied)
		{
			switch (Bomb.GetSolvedModuleNames().Count())
			{
				case 0: case 1: case 2: case 3:
					GatheredNumber += RuleNumbers[4][0];
					break;
				case 4: case 5: case 6:
					GatheredNumber += RuleNumbers[4][1];
					break;
				case 7: case 8: case 9:
					GatheredNumber += RuleNumbers[4][2];
					break;
				case 10: case 11: case 12:
					GatheredNumber += RuleNumbers[4][3];
					break;
				case 13: case 14: case 15:
					GatheredNumber += RuleNumbers[4][4];
					break;
				default:
					GatheredNumber += RuleNumbers[4][5];
					break;
			}
		}
	}
	
	void GetBallValue()
	{
		rValue = 0;
		gValue = 0;
		bValue = 0;
        for (int x = 0; x < BallColors.Length; x++)
		{
            if (BallColors[x].GetComponent<MeshRenderer>().material.color.r == 1){
                rValue++;
            } 
            if (BallColors[x].GetComponent<MeshRenderer>().material.color.g == 1){
                gValue++;
            } 
            if (BallColors[x].GetComponent<MeshRenderer>().material.color.b == 1){
                bValue++;
            }
        }
        BallValue = (1 * (rValue != 0 ? rValue : 1) * (gValue != 0 ? gValue : 1) * (bValue != 0 ? bValue : 1)) % 5;
    }
	
	IEnumerator HoldBallCoroutine()
	{
		yield return new WaitForSecondsRealtime(0.5f);
		while (HoldValue != 26)
		{
			HoldValue += 1;
			TextAndShadow[0].text = "Power: " + HoldValue.ToString();
			TextAndShadow[1].text = "Power: " + HoldValue.ToString();	
			yield return new WaitForSecondsRealtime(0.5f);
		}
		TextAndShadow[0].text = "Power: ???";
		TextAndShadow[1].text = "Power: ???";
	}
	
	IEnumerator Toss()
	{
		Checking = true;
		if (HoldValue != PowerNeeded + 1)
		{
			IncorrectAnswer = true;
		}
		Audio.PlaySoundAtTransform(SFX[0].name, transform);
		AbleToHillBall = false;
		Debug.LogFormat("[Hole in One #{0}] You hit the ball with a power level of {1}", moduleId, HoldValue != 27 ? HoldValue.ToString() : "???");
		BallHold.GetComponent<KMSelectable>().AddInteractionPunch(((float)HoldValue/10f));
		BallHold.SetActive(false);
		yield return new WaitForSecondsRealtime(3f);
		if (IncorrectAnswer)
		{
			Debug.LogFormat("[Hole in One #{0}] You did something incorrectly. The module striked and performed a reset.", moduleId);
			Audio.PlaySoundAtTransform(SFX[3].name, transform);
			yield return new WaitForSecondsRealtime(0.75f);
			Module.HandleStrike();
			HoldValue = 0;
			TextAndShadow[0].text = "Power: 0";
			TextAndShadow[1].text = "Power: 0";
			BallHold.SetActive(true);
			Clicked = false;
			IncorrectAnswer = false;
			Start();
			Proceed();
		}
		
		else
		{
			Debug.LogFormat("[Hole in One #{0}] You did everything correctly. The module solved.", moduleId);
			Audio.PlaySoundAtTransform(SFX[2].name, transform);
			yield return new WaitForSecondsRealtime(0.75f);
			Audio.PlaySoundAtTransform(SFX[1].name, transform);
			Module.HandlePass();
			TextAndShadow[0].text = "Hole In One";
			TextAndShadow[1].text = "Hole In One";
		}
		Checking = false;
	}
	
	//twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"To change the golf club being shown in the module, use the command !{0} left/right | To lock-in the golf club being shown, use the command !{0} select | To select a ball in the module, use the command !{0} ball [1-5] | To hit the ball sitting on the tee, use the command !{0} hit [1-26]";
    #pragma warning restore 414
	
	string[] ValidNumbers = {"1", "2", "3", "4", "5"};
    IEnumerator ProcessTwitchCommand(string command)
    {
		string[] parameters = command.Split(' ');
			
        if (Regex.IsMatch(command, @"^\s*right\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;
			if (Clicked)
			{
				yield return "sendtochaterror You are unable to perform this currently. The command was not processed.";
				yield break;
			}
			ArrowsAndCircle[2].OnInteract();
		}
		
		if (Regex.IsMatch(command, @"^\s*select\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;
			if (Clicked)
			{
				yield return "sendtochaterror You are unable to perform this currently. The command was not processed.";
				yield break;
			}
			ArrowsAndCircle[1].OnInteract();
		}
		
		if (Regex.IsMatch(command, @"^\s*left\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;
			if (Clicked)
			{
				yield return "sendtochaterror You are unable to perform this currently. The command was not processed.";
				yield break;
			}
			ArrowsAndCircle[0].OnInteract();
		}
		
		if (Regex.IsMatch(parameters[0], @"^\s*ball\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;
			if (!Pressable)
			{
				yield return "sendtochaterror You are unable to perform this currently. The command was not processed.";
				yield break;
			}
			
			if (parameters.Length != 2)
			{
				yield return "sendtochaterror Invalid parameter length. The command was not processed.";
				yield break;
			}
			
			
			if (!parameters[1].EqualsAny(ValidNumbers))
			{
				yield return "sendtochaterror Ball number being selected was invalid. The command was not processed.";
				yield break;
			}
			
			BallColors[Int32.Parse(parameters[1]) - 1].GetComponent<KMSelectable>().OnInteract();
		}
		
		if (Regex.IsMatch(parameters[0], @"^\s*hit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;
			if (!AbleToHillBall)
			{
				yield return "sendtochaterror You are unable to perform this currently. The command was not processed.";
				yield break;
			}
			
			if (parameters.Length != 2)
			{
				yield return "sendtochaterror Invalid parameter length. The command was not processed.";
				yield break;
			}
			
			if (parameters[1].Length > 2)
			{
				yield return "sendtochaterror Number will never reach longer than 3 digits. The command was not processed.";
				yield break;
			}
			
			int Out;
			if (!Int32.TryParse(parameters[1], out Out))
			{
				yield return "sendtochaterror Number being sent was invalid. The command was not processed.";
				yield break;
			}
			
			if (Out < 1 || Out > 26)
			{
				yield return "sendtochaterror Number being sent was not between 1-26. The command was not processed.";
				yield break;
			}
			
			BallHold.GetComponent<KMSelectable>().OnInteract();
			while (HoldValue != Out)
			{
				yield return null;
			}
			BallHold.GetComponent<KMSelectable>().OnInteractEnded();
			if (IncorrectAnswer)
				yield return "strike";
			else
				yield return "solve";
		}
	}

	IEnumerator TwitchHandleForcedSolve()
    {
		if (IncorrectAnswer)
        {
			StopAllCoroutines();
			Everything1.SetActive(false);
			Everything2.SetActive(true);
			BallHold.SetActive(false);
			Audio.PlaySoundAtTransform(SFX[1].name, transform);
			Module.HandlePass();
			TextAndShadow[0].text = "Hole In One";
			TextAndShadow[1].text = "Hole In One";
			yield break;
        }
		if (!Checking)
        {
			if (!Clicked)
			{
				while (StickNum != (GatheredNumber % 5))
				{
					if (StickNum < (GatheredNumber % 5))
					{
						ArrowsAndCircle[2].OnInteract();
						yield return new WaitForSecondsRealtime(0.1f);
					}
					else if (StickNum > (GatheredNumber % 5))
					{
						ArrowsAndCircle[0].OnInteract();
						yield return new WaitForSecondsRealtime(0.1f);
					}
				}
				ArrowsAndCircle[1].OnInteract();
			}
			if (!AbleToHillBall)
			{
				while (!Pressable) yield return true;
				BallColors[BallValue].GetComponent<KMSelectable>().OnInteract();
				yield return new WaitForSecondsRealtime(0.1f);
			}
			BallHold.GetComponent<KMSelectable>().OnInteract();
			while (HoldValue != PowerNeeded + 1) yield return null;
			BallHold.GetComponent<KMSelectable>().OnInteractEnded();
		}
		while (Checking) yield return true;
	}
}
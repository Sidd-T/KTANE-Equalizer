using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class equalizerScript : MonoBehaviour {

	public KMAudio Audio;
	public KMBombInfo Bomb;
	public KMBombModule Module;

	public KMSelectable[] btn;
	public MeshRenderer[] btnColours;
	private Color onColour = new Color(1f, 0.54902f, 0.0666666f, 1f);
	private Color offColour = new Color(0.69803f, 0.69803f, 0.69803f, 1f);
	private bool[] btnStatus = {true, true, true, true, false, true};

	public TextMesh lowFreq;
	public TextMesh hiFreq;
	private string[] freqValues = {"10 Hz", "20 Hz", "50 Hz", "100 Hz", "200 Hz", "500 Hz", "1000 Hz", 
									"2000 Hz", "5000 Hz", "10000 Hz"};
	private int[] freqNums = {10, 20, 50, 100, 200, 500, 1000, 2000, 5000, 10000};
	private int lowFreqIdx = 0;
	private int hiFreqIdx = 9;
	private int lowFreqNum, hiFreqNum;


	public TextMesh[] gains;
	private int lowGain, midGain, hiGain;
	private int increment;

	static int moduleIDCounter = 1;
	static int moduleID = 0;
	private bool isSolved = false, lightsOn = false;

	private readonly string[] instruments = {"Piano", "Piccolo", "Violin", "Bass", "Drums"}; 
	private int instCode;

	private readonly string[,] genres = new string[,]{
		{"Jazz", "Anime", "Classical"},
		{"None", "None", "None"},
		{"None", "None", "None"},
		{"EDM", "Jazz", "Rock"},
		{"Rock", "Trap", "Jazz"},
	}; 
	private int genreCode;

	private readonly int[] avgFreqs = {440, 1600, 525, 250, 110};
	private readonly string[] classifications = {"mid", "high", "mid", "low", "low"};

	void Awake(){
		//freq bands
		btn[0].OnInteract += delegate (){
            adjustFreqBand(0);
            return false;
		};
		btn[1].OnInteract += delegate (){
            adjustFreqBand(1);
            return false;
		};
		btn[2].OnInteract += delegate (){
            adjustFreqBand(2);
            return false;
		};

		//24 or 48 db filter cutoff mode
		btn[3].OnInteract += delegate (){
            modeSwitch(0);
            return false;
		};
		btn[4].OnInteract += delegate (){
            modeSwitch(1);
            return false;
		};

		//Low freq 
		btn[5].OnInteract += delegate (){
            adjustFreqSplit(ref lowFreq, ref lowFreqIdx, ref lowFreqNum, -1, 0);
            return false;
		};
		btn[6].OnInteract += delegate (){
            adjustFreqSplit(ref lowFreq, ref lowFreqIdx, ref lowFreqNum, 1, hiFreqIdx-1);
            return false;
		};
		//High freq
		btn[7].OnInteract += delegate (){
            adjustFreqSplit(ref hiFreq, ref hiFreqIdx, ref hiFreqNum, -1, lowFreqIdx+1);
            return false;
		};
		btn[8].OnInteract += delegate (){
            adjustFreqSplit(ref hiFreq, ref hiFreqIdx, ref hiFreqNum, 1, 9);
            return false;
		};

		//Low gain
		btn[9].OnInteract += delegate (){
            adjustGain(ref gains[0], ref lowGain, -1);
            return false;
		};
		btn[10].OnInteract += delegate (){
            adjustGain(ref gains[0], ref lowGain, 1);
            return false;
		};
		//Mid Gain
		btn[11].OnInteract += delegate (){
            adjustGain(ref gains[1], ref midGain, -1);
            return false;
		};
		btn[12].OnInteract += delegate (){
            adjustGain(ref gains[1], ref midGain, 1);
            return false;
		};
		//High gain
		btn[13].OnInteract += delegate (){
            adjustGain(ref gains[2], ref hiGain, -1);
            return false;
		};
		btn[14].OnInteract += delegate (){
            adjustGain(ref gains[2], ref hiGain, 1);
            return false;
		};

		//checking answer
		btn[15].OnInteract += delegate (){
            checkAnswer();
            return false;
		};

	
	}

	void adjustFreqBand(int b){

		if (!lightsOn || isSolved) return;

		btnStatus[b] = !btnStatus[b];

		Debug.LogFormat("Equalizer {0}: button {1} has new status {2}", moduleID, b, btnStatus[b]);

		if (btnStatus[b]){
			btnColours[b].GetComponent<MeshRenderer>().material.color = onColour;
		}
		else{
			btnColours[b].GetComponent<MeshRenderer>().material.color = offColour;
		}

		return;
	}

	void modeSwitch(int m){

		if (!lightsOn || isSolved) return;

		if (m == 0 && btnStatus[3] == false){
			btnStatus[3] = true;
			btnStatus[4] = false;

			btnColours[3].GetComponent<MeshRenderer>().material.color = onColour;
			btnColours[4].GetComponent<MeshRenderer>().material.color = offColour;
			
		}
		else if (m == 1 && btnStatus[4] == false){
			btnStatus[3] = false;
			btnStatus[4] = true;

			btnColours[3].GetComponent<MeshRenderer>().material.color = offColour;
			btnColours[4].GetComponent<MeshRenderer>().material.color = onColour;
		}
		else return;
	}

	void adjustFreqSplit(ref TextMesh screen, ref int idx, ref int freqNum, int dir, int boundary){

		if (!lightsOn || isSolved) return;

		if ((idx == 0 && dir == -1)||(idx == 9 && dir == 1)){
			return;
		}
		else if (boundary != 0 && boundary != 9 && idx == boundary){
			return;
		}
		else{
			idx += dir;
			screen.text = freqValues[idx];
			freqNum = freqNums[idx];
		}

		return;
	}

	void adjustGain(ref TextMesh screen, ref int gainNum, int dir){

		if (!lightsOn || isSolved) return;

		string currDisplay = screen.text;
		string valueStr = currDisplay.Substring(1, 1);

		if (currDisplay.Substring(0, 2) == "+6" && dir == 1){
			return;
		}
		if (currDisplay.Substring(0, 2) == "-6" && dir == -1){
			return;
		}
		

		int valueNum;
		bool isParsable = Int32.TryParse(valueStr, out valueNum);
		if (!isParsable){
			screen.text = "+0 dB";
			return;
		}

		string currSign = currDisplay.Substring(0, 1);
		if (currSign == "-"){
			valueNum *= -1;
		}

		Debug.LogFormat("Equalizer {0}: Adjusting gain with direction {1}, increment {2}", moduleID, dir, increment);
		Debug.LogFormat("Equalizer {0}: Current gain is {1}", moduleID, valueNum);

		int d = dir*increment;
		valueNum += d;
		if(valueNum > 6 || valueNum < -6){
			Debug.LogFormat("Equalizer {0}: Value out of bounds, returning...", moduleID);
			return;
		}

		Debug.LogFormat("Equalizer {0}: Change in value is {1}", moduleID, d);

		string newSign = "+";
		if (valueNum < 0){
			newSign = "";
		}
		string units = " dB";

		Debug.LogFormat("Equalizer {0}: New gain is {1}", moduleID, valueNum);

		string newDisplay = newSign + valueNum + units;

		screen.text = newDisplay;
		gainNum = valueNum;

		return;
	}

	bool checkPrimaryBand(){

		bool isCorrect = false;

		string band = classifications[instCode];
		int freq = avgFreqs[instCode];

		Debug.LogFormat("Equalizer {0}: Average Freq is {1}, class is {2}, low freq is {3}, hi freq is {4}", moduleID, freq, band, lowFreqNum, hiFreqNum);

		if (band == "low" && lowFreqNum >= freq){
			isCorrect = true;
		}
		else if (band == "mid" && lowFreqNum <= freq && freq <= hiFreqNum){
			isCorrect = true;
		}
		else if (band == "high" && hiFreqNum <= freq){
			isCorrect = true;
		}

		return isCorrect;
	}

	bool checkBandCut(){
		
		bool isCorrect = false;

		string band = classifications[instCode];

		bool lowCut = !btnStatus[0];
		bool hiCut = !btnStatus[2];

		Debug.LogFormat("Equalizer {0}: band is {1}, low cut is {2}, hi cut is {3}", moduleID, band, lowCut, hiCut);

		if (band == "low"){

			if (hiCut){
				if(instCode == 3 && genreCode == 0){
					if (hiFreqNum <= 1600) isCorrect = true;
				}
				else if(instCode == 3 && genreCode == 1){
					if (hiFreqNum <= 2100) isCorrect = true;
				}
				else if(instCode == 3 && genreCode == 2){
					if (hiFreqNum <= 2100) isCorrect = true;
				}
				else if(instCode == 4 && genreCode == 0){
					if (hiFreqNum <= 2000) isCorrect = true;
				}
				else if(instCode == 4 && genreCode == 1){
					if (hiFreqNum <= 1800) isCorrect = true;
				}
				else if(instCode == 4 && genreCode == 2){
					isCorrect = false;
				}
			}
			else{
				if(instCode == 4 && genreCode == 2) isCorrect = true;
			}
			
		}
		else if (band == "mid"){

			if (instCode == 0 && genreCode == 0){
				isCorrect = true;
			}
			else if (instCode == 0 && genreCode == 1){
				if(lowCut && lowFreqNum >= 200){
					isCorrect = true;
				}
			}
			else if (instCode == 0 && genreCode == 2){
				isCorrect = true;
			}
			else if (instCode == 2){
				if (midGain > lowGain && midGain > hiGain && !lowCut && !hiCut){
					isCorrect = true;
				}
			}

		}
		else if (band == "high" && lowCut){
			isCorrect = true;
		}

		return isCorrect;
	}

	bool checkSecondaryBand(){

		bool isCorrect = false;

		string primaryBand = classifications[instCode];

		Debug.LogFormat("Equalizer {0}: lowFreq {1}, hiFreq {2}, lowGain {3}, midGain {4}, hiGain {5}", 
							moduleID, lowFreqNum, hiFreqNum, lowGain, midGain, hiGain);

		if (instCode == 0 && genreCode == 0){
			if(lowFreqNum >= 200 && (hiGain-lowGain) == 3){
				isCorrect = true;
			}
		}
		else if (instCode == 0 && genreCode == 1){
			if(hiFreqNum <= 1400 && (midGain-hiGain) < 3){
				isCorrect = true;
			}
		}
		else if (instCode == 0 && genreCode == 2){
			isCorrect = true;
		}
		else if (instCode == 1){
			if(lowFreqNum == 500 && (hiGain-midGain) < 5){
				isCorrect = true;
			}
		}
		else if (instCode == 2){
			isCorrect = true;
		}
		else if (instCode == 3 && genreCode == 0){
			if ((lowGain-midGain) == 4){
				isCorrect = true;
			}
		}
		else if (instCode == 3 && genreCode == 1){
			if ((lowGain-midGain) < 3){
				isCorrect = true;
			}
		}
		else if (instCode == 3 && genreCode == 2){
			if ((lowGain-midGain) < 4){
				isCorrect = true;
			}
		}
		else if (instCode == 4 && genreCode == 0){
			if(hiFreqNum == 2000 && (lowGain-midGain) < 4){
				isCorrect = true;
			}
		}
		else if (instCode == 4 && genreCode == 1){
			if((lowGain-midGain) == 4){
				isCorrect = true;
			}
		}
		else if (instCode == 4 && genreCode == 2){
			if(hiFreqNum <= 2000 && (lowGain-midGain) < 3){
				isCorrect = true;
			}
		}


		bool lowCut = !btnStatus[0];
		bool hiCut = !btnStatus[2];

		if(primaryBand == "low"){
			if (lowGain < midGain || (lowGain < hiGain && !hiCut)){
				isCorrect = false;
			}
		}
		else if(primaryBand == "mid"){
			if ((midGain < lowGain && !lowCut) || (midGain < hiGain && !hiCut)){
				isCorrect = false;
			}
		}
		else if(primaryBand == "high"){
			if (hiGain < midGain || (hiGain < lowGain && !lowCut)){
				isCorrect = false;
			}
		}

		return isCorrect;
	}

	bool checkRollOff(){
		bool isCorrect = true;

		bool hiCut = !btnStatus[2];

		if(hiCut && !btnStatus[4])
			isCorrect = false;

		return isCorrect;

	}

	void checkAnswer(){

		if (!lightsOn || isSolved) return;

		bool isCorrect = checkPrimaryBand();
		if(isCorrect){
			Debug.LogFormat("Equalizer {0}: Primary Band correct", moduleID);
			isCorrect = checkBandCut();
		}
		else{
			Debug.LogFormat("Equalizer {0}: Primary Band incorrect", moduleID);
		}
		if(isCorrect){
			Debug.LogFormat("Equalizer {0}: Band cut correct", moduleID);
			isCorrect = checkSecondaryBand();
		}
		else{
			Debug.LogFormat("Equalizer {0}: Band cut incorrect", moduleID);
		}
		if(isCorrect){
			Debug.LogFormat("Equalizer {0}: Secondary Band correct", moduleID);
			isCorrect = checkRollOff();
		}
		else{
			Debug.LogFormat("Equalizer {0}: Secondary Band incorrect", moduleID);
		}

		if (isCorrect){
			Debug.LogFormat("Equalizer {0}: Answer correct! Module passed!", moduleID);
            Audio.PlaySoundAtTransform("correct", Module.transform);
			
			btnStatus[5] = false;
			btnColours[5].GetComponent<MeshRenderer>().material.color = offColour;
            Module.HandlePass();
            isSolved = true;
        }
		else{
			Debug.LogFormat("Equalizer {0}: Answer incorrect! Strike!", moduleID);
            Audio.PlaySoundAtTransform("strike", Module.transform);
            Module.HandleStrike();
		}

		return;
	}

	// Use this for initialization
	void Start () {
		moduleID = moduleIDCounter++;
		GetComponent<KMBombModule>().OnActivate += Init;
	}

	void Init(){

		Debug.LogFormat("Equalizer {0}:", moduleID);
		setInitButtons();
		setInitGains();
		setInitFreqSplits();

		getIncrement();
		getInstrument();
		getGenre(instCode);
		lightsOn = true;
	}
	
	void setInitButtons(){

		Debug.LogFormat("Equalizer {0}: Setting buttons", moduleID);

		for (int i = 0; i < 6; i++){
			if (btnStatus[i]){
				btnColours[i].GetComponent<MeshRenderer>().material.color = onColour;
			}
			else{
				btnColours[i].GetComponent<MeshRenderer>().material.color = offColour;
			}
		}

		return;
	}

	void setInitGains(){

		Debug.LogFormat("Equalizer {0}: Setting gains", moduleID);

		int low = UnityEngine.Random.Range(-4, 5);
		int mid = UnityEngine.Random.Range(-4, 5);
		int high = UnityEngine.Random.Range(-4, 5);

		string lowSign = "+";
		if (low < 0) lowSign = "";

		string midSign = "+";
		if (mid < 0) midSign = "";

		string highSign = "+";
		if (high < 0) highSign = "";

		string units = " dB";

		string lowDisp = lowSign + low + units;
		string midDisp = midSign + mid + units;
		string highDisp = highSign + high + units;

		gains[0].text = lowDisp;
		gains[1].text = midDisp;
		gains[2].text = highDisp;
		lowGain = low;
		midGain = mid;
		hiGain = high;

		return;
	}


	void setInitFreqSplits(){

		Debug.LogFormat("Equalizer {0}: Setting frequency splits", moduleID);

		lowFreqIdx = 0;
		hiFreqIdx = 9;
		lowFreq.text = freqValues[lowFreqIdx];
		hiFreq.text = freqValues[hiFreqIdx];
		hiFreqNum = 10000;
		lowFreqNum = 10;
	}

	void getIncrement(){
		int r = UnityEngine.Random.Range(1, 101);

		if (r >= 40)
			increment = 1;
		else
			increment = 2;
	}

	void getInstrument(){

		Debug.LogFormat("Equalizer {0}: Getting the instrument...", moduleID);

		if (Bomb.IsPortPresent(Port.Parallel)){
			instCode = 0;
			Debug.LogFormat("Equalizer {0}: The instrument is {1}, instCode is {2}", moduleID, instruments[instCode], instCode);
		}
		else if (Bomb.IsIndicatorOn(Indicator.CAR) || Bomb.IsIndicatorOn(Indicator.FRK)){
			instCode = 1;
			Debug.LogFormat("Equalizer {0}: The instrument is {1}, instCode is {2}", moduleID, instruments[instCode], instCode);
		}
		else if (Bomb.GetSerialNumberLetters().Any("VIOLN".Contains)){
			instCode = 2;
			Debug.LogFormat("Equalizer {0}: The instrument is {1}, instCode is {2}", moduleID, instruments[instCode], instCode);
		}
		else if (increment == 1){
			instCode = 3;
			Debug.LogFormat("Equalizer {0}: The instrument is {1}, instCode is {2}", moduleID, instruments[instCode], instCode);
		}
		else{
			instCode = 4;
			Debug.LogFormat("Equalizer {0}: The instrument is {1}, instCode is {2}", moduleID, instruments[instCode], instCode);
		}

		return;
	}

	void getGenre(int instCode){

		int count = Bomb.GetBatteryCount();
		Debug.LogFormat("Equalizer {0}: Getting the genre, the instCode is {1}, there are {2} batteries", moduleID, instCode, count);
		

		if (count == 0 || count == 1){
			genreCode = 0;
			Debug.LogFormat("Equalizer {0}: The genre is {1}...", moduleID, genres[instCode, genreCode]);
		}
		else if (count == 2 || count == 3){
			genreCode = 1;
			Debug.LogFormat("Equalizer {0}: The genre is {1}...", moduleID, genres[instCode, genreCode]);
		}
		else {
			genreCode = 2;
			Debug.LogFormat("Equalizer {0}: The genre is {1}...", moduleID, genres[instCode, genreCode]);
		}

		if ((instCode == 0 && genreCode == 0)||(instCode == 3 && genreCode == 0)||(instCode == 4 && genreCode == 1))
			increment = 1;

		return;
	}

}

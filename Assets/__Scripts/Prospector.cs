﻿using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class Prospector : MonoBehaviour {

	static public Prospector 	S;

	[Header("Set in Inspector")]
	public TextAsset deckXML;
    public TextAsset layoutXML;
    public float xOffset = 3;
    public float yOffset = -2.5f;
    public Vector3 layoutCenter;
    public Vector2 fsPosMid = new Vector2(0.5f, 0.90f);
    public Vector2 fsPosRun = new Vector2(0.5f, 0.75f);
    public Vector2 fsPosMid2 = new Vector2(0.4f, 1.0f);
    public Vector2 fsPosEnd = new Vector2(0.5f, 0.95f);
    public float reloadDelay = 2f; // 2 sec delay between rounds
    public Text gameOverText, roundResultText, highScoreText;


    [Header("Set Dynamically")]
	public Deck	deck;
    public Layout layout;
    public List<CardProspector> drawPile;
    public Transform layoutAnchor;
    public CardProspector target;
    public List<CardProspector> tableau;
    public List<CardProspector> discardPile;
    public FloatingScore fsRun;

	void Awake(){
		S = this;
        SetUpUITexts();
	}

    void SetUpUITexts()
    {
        // set up the highscore ui text
        GameObject go = GameObject.Find("HighScore");
        if(go != null)
        {
            highScoreText = go.GetComponent<Text>();
        }
        int highScore = ScoreManager.HIGH_SCORE;
        string hScore = "High Score: " + Utils.AddCommasToNumber(highScore);
        go.GetComponent<Text>().text = hScore;

        // set up the ui texts that show at the end of the round
        go = GameObject.Find("GameOver");
        if(go != null)
        {
            gameOverText = go.GetComponent<Text>();
        }

        go = GameObject.Find("RoundResult");
        if(go != null)
        {
            roundResultText = go.GetComponent<Text>();
        }

        // make the end of the round texts invisible
        ShowResultsUI(false);
    }

    void ShowResultsUI(bool show)
    {
        gameOverText.gameObject.SetActive(show);
        roundResultText.gameObject.SetActive(show);
    }

	void Start() {
        Scoreboard.S.score = ScoreManager.SCORE;

		deck = GetComponent<Deck> (); // get the deck
		deck.InitDeck (deckXML.text); // pass DeckXML to it
        Deck.Shuffle(ref deck.cards); // this shuffles the deck by reference

        Card c;
        for(int cNum = 0; cNum<deck.cards.Count; cNum++)
        {
            c = deck.cards[cNum];
            c.transform.localPosition = new Vector3((cNum % 13) * 3, cNum / 13 * 4, 0);
        }

        layout = GetComponent<Layout>(); // get the layout component
        layout.ReadLayout(layoutXML.text); // pass layoutxml to it

        drawPile = ConvertListCardsToListCardProspectors(deck.cards);
        LayoutGame();
	}

    List<CardProspector> ConvertListCardsToListCardProspectors(List<Card> lCD)
    {
        List<CardProspector> lCP = new List<CardProspector>();
        CardProspector tCP;
        foreach (Card tCD in lCD)
        {
            tCP = tCD as CardProspector;
            lCP.Add(tCP);
        }
        return (lCP);
    }

    // the draw function will pull a single card from the drawpile and return it
    CardProspector Draw()
    {
        CardProspector cd = drawPile[0]; // pull the 0th CardProspector
        drawPile.RemoveAt(0); // then remove it from list<> drawpile
        return (cd); // and return it
    }

    // layoutgame() positions the initial tableau of cards, a.k.a. the "mine"
    void LayoutGame()
    {
        // create an empty gameobject to serve as an anchor for the tableau
        if(layoutAnchor == null)
        {
            GameObject tGO = new GameObject("_LayoutAnchor");
            // ^ create an empty gameobject named _layoutanchor in the hierarchy
            layoutAnchor = tGO.transform; // grab its transform
            layoutAnchor.transform.position = layoutCenter; // position it
        }

        CardProspector cp;
        // follow the layout
        foreach(SlotDef tSD in layout.slotDefs)
        {
            // ^ iterate through all the slotdefs in the layout.slotdefs as tSD
            cp = Draw(); // pull a card from the top (beginning) of the draw pile
            cp.faceUp = tSD.faceUp; // set its faceUp to the value in slotdef
            cp.transform.parent = layoutAnchor; // make its parent layoutanchor
            // this replaces the previous parent: deck.deckAnchor, which appears as _Deck in the hierarchy when the scene is playing
            cp.transform.localPosition = new Vector3(layout.multiplier.x * tSD.x, layout.multiplier.y * tSD.y, -tSD.layerID);
            // ^ set the localposition of the card based on slotdef
            cp.layoutID = tSD.id;
            cp.slotDef = tSD;
            // cardprospectors in the tableau have the state cardstate.tableau
            cp.state = eCardState.tableau;
            // cardprospectors in the tableau have the state cardstate.tableau
            cp.SetSortingLayerName(tSD.layerName); // set the sorting layers

            tableau.Add(cp); // add this cardprospector to the list<> tableau

        }

        // set which cards are hiding others
        foreach(CardProspector tCP in tableau)
        {
            foreach(int hid in tCP.slotDef.hiddenBy)
            {
                cp = FindCardByLayoutID(hid);
                tCP.hiddenBy.Add(cp);
            }
        }

        // set up the initial target card
        MoveToTarget(Draw());

        // set up the draw pile
        UpdateDrawPile();
    }

    // convert from the layoutID int to the cardprospector with that ID
    CardProspector FindCardByLayoutID(int layoutID)
    {
        foreach(CardProspector tCP in tableau)
        {
            // search through all cards in the tableau List<>
            if(tCP.layoutID == layoutID)
            {
                // if the card has the same ID, return it
                return (tCP);
            }
        }
        // if it's not found, return null
        return (null);
    }

    // this turns cards in the mine face up or face down
    void SetTableauFaces()
    {
        foreach(CardProspector cd in tableau)
        {
            bool faceUp = true; // assume the card will be face up
            foreach(CardProspector cover in cd.hiddenBy)
            {
                // if either of the covering cards are in the tableau
                if(cover.state == eCardState.tableau)
                {
                    faceUp = false; // then this card is face down
                }
            }
            cd.faceUp = faceUp; // Set the value on the card
        }
    }

    // Moves the current target to the discardPile
    void MoveToDiscard(CardProspector cd)
    {
        // Set the state of the card to discard
        cd.state = eCardState.discard;
        discardPile.Add(cd); // Add it to the discardPile List<>
        cd.transform.parent = layoutAnchor; // Update its transform parent

        // Position this card on the discardPile
        cd.transform.localPosition = new Vector3(
            layout.multiplier.x * layout.discardPile.x,
            layout.multiplier.y * layout.discardPile.y,
            -layout.discardPile.layerID + 0.5f);
        cd.faceUp = true;
        // Place it on top of te pile for depth sorting
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(-100 + discardPile.Count);
    }

    // Make cd the new target card 
    void MoveToTarget(CardProspector cd)
    {
        // If there is currently a target card, movie it to discardPile
        if (target != null) MoveToDiscard(target);
        target = cd; // cd is te new target
        cd.state = eCardState.target;
        cd.transform.parent = layoutAnchor;

        // Move to the target position
        cd.transform.localPosition = new Vector3(
            layout.multiplier.x * layout.discardPile.x,
            layout.multiplier.y * layout.discardPile.y,
            -layout.discardPile.layerID);

        cd.faceUp = true; // Make it face-up
                          // Set the depth sorting
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(0);
    }

    // Arranges all the cards of the drawPile to show how many are left
    void UpdateDrawPile()
    {
        CardProspector cd;
        // Go through all the cards of the drawPile
        for (int i = 0; i < drawPile.Count; i++)
        {
            cd = drawPile[i];
            cd.transform.parent = layoutAnchor;

            if (Random.Range(0, 100) < 10)
                drawPile[i].isGoldCard = true;

            // Position it correctly with the layout.drawPile.stagger
            Vector2 dpStagger = layout.drawPile.stagger;
            cd.transform.localPosition = new Vector3(
                layout.multiplier.x * (layout.drawPile.x + i * dpStagger.x),
                layout.multiplier.y * (layout.drawPile.y + i * dpStagger.y),
                -layout.drawPile.layerID + 0.1f * i);

            cd.faceUp = false; // Make them all face-down
            cd.state = eCardState.drawpile;
            // Set depth sorting
            cd.SetSortingLayerName(layout.drawPile.layerName);
            cd.SetSortOrder(-10 * i);
        }
    }

    // cardclicked is called any time a card in the game is clicked 
    public void CardClicked(CardProspector cd)
    {
        // the reaction is determined by the state of the clicked card
        switch(cd.state)
        {
            case eCardState.target:
                // clicking the target card does nothing
                break;

            case eCardState.drawpile:
                // clicking any card in the drawpile will draw the next card
                MoveToDiscard(target); // moves the target to the discardpile
                MoveToTarget(Draw()); // moves the next drawn card to the target
                UpdateDrawPile(); // restacks the drawPile
                ScoreManager.EVENT(eScoreEvent.draw, false);
                FloatingScoreHandler(eScoreEvent.draw);
                break;

            case eCardState.tableau:
                // clicking a card in the tableau will check if it's a valid play
                bool validMatch = true;
                CardProspector tempCardRef = target;
                if(!cd.faceUp)
                {
                    // if the card if face down, it's not valid
                    validMatch = false;
                }
                if(!AdjacentRank(cd, target))
                {
                    // if it's not an adjacent rank, it's not valid
                    validMatch = false;
                }
                if (!validMatch) return; // return if not valid

                // if we got here, then: yay! it's a valid card
                tableau.Remove(cd); // remove it from the tableau list
                MoveToTarget(cd); // make it the target card
                SetTableauFaces(); // update tableau card face ups

                ScoreManager.EVENT(eScoreEvent.mine, tempCardRef.isGoldCard);
                FloatingScoreHandler(eScoreEvent.mine);
                break;
        }
        // check to see whether the game is over or not
        CheckForGameOver();
    }

    // test whether the game is over
    void CheckForGameOver()
    {
        // if the tableau is empty, the game is over
        if(tableau.Count == 0)
        {
            // call gameover() with a win
            GameOver(true);
            return;
        }

        // if there are still cards int eh draw pile, the game's not over
        if(drawPile.Count>0)
        {
            return;
        }

        // check for remaining valid players
        foreach(CardProspector cd in tableau)
        {
            if(AdjacentRank(cd, target))
            {
                // if there is a valid play, the game's not over
                return;
            }
        }

        // since there are no valid plays, the game is over
        // call gameover with a loss
        GameOver(false);
    }

    // called when the game is over. simple for now, but expandable
    void GameOver(bool won)
    {
        int score = ScoreManager.SCORE;
        if (fsRun != null) score += fsRun.score;
        if(won)
        {
            gameOverText.text = "Round Over";
            roundResultText.text = "You won this round!\nRound Score: "+score;
            ShowResultsUI(true);
            // print("Game Over. You won! :)");
            ScoreManager.EVENT(eScoreEvent.gameWin, false);
            FloatingScoreHandler(eScoreEvent.gameWin);
        }
        else
        {
            gameOverText.text = "Game Over";
            if(ScoreManager.HIGH_SCORE <= score)
            {
                string str = "You got the high score!\nHigh score: "+score;
                roundResultText.text = str;
            }
            else
            {
                roundResultText.text = "Your final score was: "+score;
            }
            ShowResultsUI(true);
            //print("Game Over. You lost. :(");
            ScoreManager.EVENT(eScoreEvent.gameLoss, false);
            FloatingScoreHandler(eScoreEvent.gameLoss);
        }
        // reload the scene, resetting the game
        // SceneManager.LoadScene("__Prospector_Scene_0");

        // reload the scene in reloaddelay seconds
        // this will give the score a moment to travel
        Invoke("ReloadLevel", reloadDelay);
    }

    void ReloadLevel()
    {
        // reload the scene, resetting the game
        SceneManager.LoadScene("__Prospector_Scene_0");
    }

    // return true if the two cards are adjacent in rank (A & K wrap around)
    public bool AdjacentRank(CardProspector c0, CardProspector c1)
    {
        // if either card is face down, it's not adjacent
        if (!c0.faceUp || !c1.faceUp) return (false);

        // if they are 1 apart, they are adjacent
        if(Mathf.Abs(c0.rank - c1.rank) == 1)
        {
            return (true);
        }
        // if one is ace and the other king, they are adjacent
        if(c0.rank == 1 && c1.rank == 13) return (true);
        if (c0.rank == 13 && c1.rank == 1) return (true);

        // otherwise, return false
        return (false);
    }

    // handle floatingscore movement
    void FloatingScoreHandler(eScoreEvent evt)
    {
        List<Vector2> fsPts;
        switch(evt)
        {
            // same things need to happen whether it's a draw, a win, or a loss
            case eScoreEvent.draw: // drawing a card
            case eScoreEvent.gameWin: // won the round
            case eScoreEvent.gameLoss: // lost the round
                // add fsrun to the scoreboard score
                if(fsRun != null)
                {
                    // create points for the bezier curve
                    fsPts = new List<Vector2>();
                    fsPts.Add(fsPosRun);
                    fsPts.Add(fsPosMid2);
                    fsPts.Add(fsPosEnd);
                    fsRun.reportFinishTo = Scoreboard.S.gameObject;
                    fsRun.Init(fsPts, 0, 1);
                    // also adjust the fontsize
                    fsRun.fontSizes = new List<float>(new float[] { 28, 36, 4 });
                    fsRun = null; // clear fsrun so it's created again
                }
                break;

            case eScoreEvent.mine: // remove a mine card
                // create a floatingscore for this score
                FloatingScore fs;
                // move it from the mouseposition to fsposrun
                Vector2 p0 = Input.mousePosition;
                p0.x /= Screen.width;
                p0.y /= Screen.height;
                fsPts = new List<Vector2>();
                fsPts.Add(p0);
                fsPts.Add(fsPosMid);
                fsPts.Add(fsPosRun);
                fs = Scoreboard.S.CreateFloatingScore(ScoreManager.CHAIN, fsPts);
                fs.fontSizes = new List<float>(new float[] { 4, 50, 28 });
                if(fsRun == null)
                {
                    fsRun = fs;
                    fsRun.reportFinishTo = null;
                }
                else
                {
                    fs.reportFinishTo = fsRun.gameObject;
                }
                break;
        }
    }

}

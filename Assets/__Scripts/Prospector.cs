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


	[Header("Set Dynamically")]
	public Deck	deck;
    public Layout layout;
    public List<CardProspector> drawPile;
    public Transform layoutAnchor;
    public CardProspector target;
    public List<CardProspector> tableau;
    public List<CardProspector> discardPile;

	void Awake(){
		S = this;
	}

	void Start() {
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
            cp.transform.localPosition = new Vector3(
                layout.multiplier.x * tSD.x,
                layout.multiplier.y * tSD.y,
                -tSD.layerID);
            // ^ set the localposition of the card based on slotdef
            cp.layoutID = tSD.id;
            cp.slotDef = tSD;
            // cardprospectors in the tableau have the state cardstate.tableau
            cp.state = eCardState.tableau;
            // cardprospectors in the tableau have the state cardstate.tableau
            cp.SetSortingLayerName(tSD.layerName); // set the sorting layers

            tableau.Add(cp); // add this cardprospector to the list<> tableau

        }
        // set up the initial target card
        MoveToTarget(Draw());

        // set up the draw pile
        UpdateDrawPile();
    }

    // moves the current target to the discardpile
    void MoveToDiscard(CardProspector cd)
    {
        // set the state of the card to discard
        cd.state = eCardState.discard;
        discardPile.Add(cd); // add it to the dicardpile list<>
        cd.transform.parent = layoutAnchor; // update its transform parent

        // position this card on the discardpile
        cd.transform.localPosition = new Vector3(
            layout.multiplier.x * layout.discardPile.x,
            layout.multiplier.y * layout.discardPile.y,
            -layout.discardPile.layerID + 0.5f);
        cd.faceUp = true;
        // place it on top of the pile for depth sorting
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(-100 + discardPile.Count);
    }

    // make cd the new target card
    void MoveToTarget(CardProspector cd)
    {
        // if there is currently a target card, move it to discardpile
        if (target != null) MoveToDiscard(target);
        target = cd; // cd is the new target
        cd.state = eCardState.target;
        cd.transform.parent = layoutAnchor;

        // move to the target position
        cd.transform.localPosition = new Vector3(
            layout.multiplier.x * layout.discardPile.x,
            layout.multiplier.y * layout.discardPile.y,
            -layout.discardPile.layerID);

        cd.faceUp = true; // make it face up
        // set the depth sorting
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(0);
    }

    // arranges all the cards of the drawpile to show how many are left
    void UpdateDrawPile()
    {
        CardProspector cd;
        // go through all the cards of the drawpile
        for(int i=0; i<drawPile.Count; i++)
        {
            cd = drawPile[i];
            cd.transform.parent = layoutAnchor;

            // position it correctly with the layout.drawpile.stagger
            Vector2 dpStagger = layout.drawPile.stagger;
            cd.transform.localPosition = new Vector3(
                layout.multiplier.x * (layout.drawPile.x + i * dpStagger.x),
                layout.multiplier.y * (layout.drawPile.y + i * dpStagger.y),
                -layout.drawPile.layerID + 0.1f * i);

            cd.faceUp = false; // make them all face down
            cd.state = eCardState.drawpile;
            // set depth sorting
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
                break;

            case eCardState.tableau:
                // clicking a card in the tableau will check if it's a valid play
                break;
        }
    }

}

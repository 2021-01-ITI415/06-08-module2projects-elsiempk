using UnityEngine;
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

            tableau.Add(cp); // add this cardprospector to the list<> tableau

        }
    }

}

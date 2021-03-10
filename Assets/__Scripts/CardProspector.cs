using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// an enum defines a variable type with a few prenamed values
public enum eCardState
{
    drawpile,
    tableau,
    target,
    discard
}

public class CardProspector : Card
{
    // make sure cardprospector extends card
    [Header("Set Dynamically: CardProspector")]
    // this is how you use the enum ecardstate
    public eCardState state = eCardState.drawpile;
    // the hiddenby list stores which other cards will keep this one face down
    public List<CardProspector> hiddenBy = new List<CardProspector>();
    // the layoutid matches this card to the tableau xml if it's a tableau card
    public int layoutID;
    // the slotdef class stores information pulled in from the layoutxml <slot>
    public SlotDef slotDef;

    // This allows the card to react to being clicked 
    override public void OnMouseUpAsButton()
    {
        // Call the CardClicked method on the Prospector singleton
        Prospector.S.CardClicked(this);
        // Also call the base class (Card.cs) version of this method
        base.OnMouseUpAsButton();
    }
}


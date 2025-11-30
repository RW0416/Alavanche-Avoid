using UnityEngine;
using UnityEngine.Animations;

public class BoardDetachOnDeath : MonoBehaviour
{
    public ParentConstraint boardConstraint;
    public Rigidbody boardRigidbody;

    public void Attach()
    {
        // normal riding state
        if (!boardConstraint) boardConstraint = GetComponent<ParentConstraint>();
        if (!boardRigidbody) boardRigidbody = GetComponent<Rigidbody>();

        boardConstraint.enabled = true;
        boardConstraint.constraintActive = true;

        boardRigidbody.isKinematic = true;
        boardRigidbody.useGravity = false;
    }

    public void Detach()
    {
        // call this when you die / ragdoll
        if (!boardConstraint) boardConstraint = GetComponent<ParentConstraint>();
        if (!boardRigidbody) boardRigidbody = GetComponent<Rigidbody>();

        boardConstraint.constraintActive = false;
        boardConstraint.enabled = false;

        // unparent so it’s fully separate from the character
        transform.SetParent(null, true);

        boardRigidbody.isKinematic = false;
        boardRigidbody.useGravity = true;
    }
}

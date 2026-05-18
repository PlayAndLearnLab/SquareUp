using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MovementController : MonoBehaviour
{

    public Transform objTransform;
    private Vector3 deltaTransform;
    public int speed;

    public bool old_IsMoving => deltaTransform != Vector3.zero;
    public bool IsMoving => deltaTransform.sqrMagnitude > 0.0001f;

    public void Awake()
    {
        objTransform = GetComponent<Transform>();
        deltaTransform = new Vector3(0, 0, 0);
    }

    public void FixedUpdate()
    {
        if (objTransform != null && deltaTransform != Vector3.zero)
        {
            objTransform.position += deltaTransform;
        }
    }

    public virtual IEnumerator WalkTo(Vector3 targetPos, int speed)
    {
        if (objTransform == null) yield break;

        float normalizedSpeed = speed / 75f;
        Vector3 currentPos = objTransform.position;
        Vector3 differenceVector = targetPos - currentPos;
        Vector3 directionVector = differenceVector.normalized;
        deltaTransform = directionVector * normalizedSpeed;

        float stoppingDistance = deltaTransform.magnitude;

        while (objTransform != null)
        {
            if (Vector3.Distance(objTransform.position, targetPos) <= stoppingDistance)
            {
                break;
            }
            yield return null;
        }

        StopMovement(targetPos);

        //if (objTransform != null)
        //{
        //    deltaTransform = new Vector3(0, 0, 0);
        //    objTransform.position = targetPos;
        //}
    }

    public void StopMovement(Vector3? finalSnapPosition = null)
    {
        deltaTransform = Vector3.zero;
        if (finalSnapPosition.HasValue && objTransform != null)
        {
            objTransform.position = finalSnapPosition.Value;
        }
    }

    public virtual IEnumerator WalkToInSecs(Vector3 targetPos, float seconds)
    {
        Vector3 currentPos = objTransform.position;
        Vector3 differenceVector = targetPos - currentPos;
        Vector3 directionVector = differenceVector.normalized;
        float distance = differenceVector.magnitude;
        float numFrames = seconds / Time.fixedDeltaTime;
        float speed = distance / numFrames;
        deltaTransform = directionVector * speed;
        yield return new WaitForSeconds(seconds);
        deltaTransform = new Vector3(0, 0, 0);
        objTransform.position = targetPos;
    }

    public virtual IEnumerator Walk(Vector3 pathVector, int speed)
    {
        yield return WalkTo(objTransform.position + pathVector, speed);
    }

    private void OnDisable()
    {
        deltaTransform = Vector3.zero;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
  float mainSpeed = 30.0f; //regular speed
  float shiftAdd = 100.0f; //multiplied by how long shift is held.  Basically running
  float maxShift = 200.0f; //Maximum speed when holdin gshift
  float camSens = 0.25f; //How sensitive it with mouse
  private Vector3 lastMouse = new Vector3(255, 255, 255); //kind of in the middle of the screen, rather than at the top (play)
  private float totalRun = 1.0f;

  void Update()
  {
    Vector3 mousePos = Mouse.current.position.ReadValue();
    mousePos.z= Camera.main.nearClipPlane;
    lastMouse = mousePos - lastMouse;
    lastMouse = new Vector3(-lastMouse.y * camSens, lastMouse.x * camSens, 0);
    lastMouse = new Vector3(transform.eulerAngles.x + lastMouse.x, transform.eulerAngles.y + lastMouse.y, 0);
    transform.eulerAngles = lastMouse;

    mousePos = Mouse.current.position.ReadValue();
    mousePos.z= Camera.main.nearClipPlane;
    lastMouse = mousePos;
    //Mouse  camera angle done.  

    //Keyboard commands
    Vector3 p = GetBaseInput();
    if (p.sqrMagnitude > 0)
    { // only move while a direction key is pressed
      if (Keyboard.current.leftShiftKey.isPressed)
      {
        totalRun += Time.deltaTime;
        p = p * totalRun * shiftAdd;
        p.x = Mathf.Clamp(p.x, -maxShift, maxShift);
        p.y = Mathf.Clamp(p.y, -maxShift, maxShift);
        p.z = Mathf.Clamp(p.z, -maxShift, maxShift);
      }
      else
      {
        totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
        p = p * mainSpeed;
      }

      p = p * Time.deltaTime;
      Vector3 newPosition = transform.position;
      if (Keyboard.current.spaceKey.isPressed)
      { //If player wants to move on X and Z axis only
        transform.Translate(p);
        newPosition.x = transform.position.x;
        newPosition.z = transform.position.z;
        transform.position = newPosition;
      }
      else
      {
        transform.Translate(p);
      }
    }
  }

  private Vector3 GetBaseInput()
  { //returns the basic values, if it's 0 than it's not active.
    Vector3 p_Velocity = new Vector3();
    if (Keyboard.current.wKey.isPressed)
    {
      p_Velocity += new Vector3(0, 0, 1);
    }
    if (Keyboard.current.sKey.isPressed)
    {
      p_Velocity += new Vector3(0, 0, -1);
    }
    if (Keyboard.current.aKey.isPressed)
    {
      p_Velocity += new Vector3(-1, 0, 0);
    }
    if (Keyboard.current.dKey.isPressed)
    {
      p_Velocity += new Vector3(1, 0, 0);
    }
    return p_Velocity;
  }
}

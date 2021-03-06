﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Car : MonoBehaviour
{
    Vector3 direction = Vector3.zero;
    float speed = 0;
    float maxSpeed = 0.15f;
    float minSpeed = 0.001f;
    public float turnRadius = 90.0f;
    public float acceleration = 5;
    public float brakePower = 0.5f;
    bool holdLeft = false;
    bool holdRight = false;
    bool holdAccelerator = false;
    bool holdBrake = false;

    public AudioSource src;

    public Image iconLeft;
    public Image iconRight;
    public Image iconAccelerator;
    public Image iconBrake;

    public GameObject panel;
    public Text panelLabel;
    public Text timeLabel;
    public Text lapLabel;
    float raceTime = 0;
    int targetIndex = 0;
    int lap = 1;
    // Start is called before the first frame update
    bool showed = false;

    public static Car instance;

    private void Awake ()
    {
        instance = this;
    }

    public void Start()
    {
        panel.SetActive(false);
        src.volume = 0.5f;
        showed = false;
        src = GetComponent<AudioSource>();
        transform.position = Vector3.one * 10000;
        transform.up = -Camera.main.transform.forward;

    }
    
    public void ResetPosition()
    {
        lap = 0;
        speed = 0;
        raceTime = 0;
        targetIndex = 0;
        NextTarget();
        if(!src.isPlaying)
        src.Play();

        if (CatmullRom.instance.controlPointsList.Count > 0)
        {
            Transform goal = CatmullRom.instance.goal;
            transform.SetPositionAndRotation(goal.position, goal.rotation);
        }
        else
        {
            transform.position = Camera.main.transform.position + Camera.main.transform.forward * 10.0f;
            transform.up = -Camera.main.transform.forward;
        }
    }

    public void MoveTo(Vector2 direction)
    {
        Debug.Log(direction);

        Vector3 dir = Camera.main.transform.TransformDirection(direction);
        Debug.DrawRay(Camera.main.transform.position, dir, Color.red, 3.0f);


        Vector3 targetPos = transform.position + dir;

        if (CatmullRom.instance.controlPointsList.Count > 0)
        {
            Transform t = CatmullRom.instance.controlPointsList[0].transform;
            targetPos = Vector3.ProjectOnPlane(targetPos - t.position, t.up) + t.position;
            Debug.DrawLine(transform.position, targetPos, Color.red, 5.0f);

            Vector3 targetDir = (targetPos - transform.position).normalized;

            Quaternion rot = Quaternion.LookRotation(-targetDir, transform.up);
            Quaternion prevRotation = transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, turnRadius * 0.05f * Time.deltaTime);
            float dot = Mathf.Min(0.2f, Vector3.Dot(transform.forward, -targetDir));
            float angle = Quaternion.Angle(transform.rotation, prevRotation) / 360.0f;
            speed += dot * acceleration * Time.deltaTime   * 0.1f;
            speed *= 1 - angle;
        }


    }

    public void Update()
    {
        raceTime += Time.deltaTime; 
        float minutes = Mathf.Floor(raceTime / 60.0f);
        float seconds = Mathf.Floor(Mathf.Repeat(raceTime, 60));
        timeLabel.text = minutes + ":" + seconds;

        //align car to the plane
        if(CatmullRom.instance.controlPointsList.Count > 0)
        {
            if(!showed)
            {
                showed = true;
                ResetPosition();
            }

            Transform t = CatmullRom.instance.controlPointsList[0].transform;
            transform.position = Vector3.ProjectOnPlane(transform.position - t.position,  t.up) + t.position;

            Vector3 forward = transform.forward;
            //Vector3 right = transform.right;
            transform.up = t.up;
            //transform.right = right;
            transform.forward = forward;
            
        }

        /////////////////////
        //controlli manuali
        /////////////////////
        if(holdLeft && Mathf.Abs(speed) > minSpeed)
            transform.RotateAround(transform.position, transform.up, - Time.deltaTime  * turnRadius);

        if(holdRight && Mathf.Abs(speed) > minSpeed)
            transform.RotateAround(transform.position, transform.up, Time.deltaTime  * turnRadius);
            
        if(holdAccelerator)
            speed += acceleration * Time.deltaTime * 0.1f;

        if(holdBrake)
            speed -= acceleration * Time.deltaTime * 0.1f;

        //limita la velocita' massima
        speed = Mathf.Clamp(speed, -maxSpeed, maxSpeed);

        //rallenta
        speed *= 0.99f;


        //rallenta di piu' se gira
        if(holdLeft || holdRight) 
            speed *= 0.99f;

        transform.position -= transform.forward * speed;
        //Debug.Log("speed: " + speed);

        //limita la posizione della macchina a un certo raggio intorno alla pista
        transform.position = Vector3.ClampMagnitude(transform.position, 10.0f);

        //regola il pitch del suono in base alla speed
        src.pitch = 0.3f + 0.7f * Mathf.Abs(speed) / maxSpeed;
        src.volume = 0.05f + 0.1f * Mathf.Abs(speed) / maxSpeed;
        
        //ferma il suono quando la macchina è ferma
        //if (Mathf.Abs(speed) <= minSpeed)
        //    src.Stop()
        

        //slow down outside
        Collider[] colls = Physics.OverlapSphere(transform.position, 0.5f);
        foreach(var col in colls)
        {
            if (col.gameObject.CompareTag("target"))
            {
                NextTarget();
            }
            else
            {
                if (speed > maxSpeed / 2.0f)
                    speed *= 0.9f;
            }
        }
    }

    public void NextTarget ()
    {
        targetIndex++;
        var pts = CatmullRom.instance.controlPointsList;
        if (targetIndex > pts.Count - 1)
            targetIndex = 0;

        if (targetIndex == 1 && pts.Count > 3)
        {
            lap++;
            lapLabel.text = "Giri: " + lap;
            if(lap >= 4)
            {
                speed = 0;
                Debug.Log("fine");
                panelLabel.text = "Tempo arrivo: " + timeLabel.text;
                panel.SetActive(true);
                src.Stop();
            }
        }

        if (pts.Count > 0)
        {
            foreach(var target in pts)
            {
                target.transform.GetChild(0).gameObject.SetActive(false);
            }

            //enable the target
            pts[targetIndex].transform.GetChild(0).gameObject.SetActive(true);
        }
    }
    public void Accelerate(bool pressed)
    {
        Vibration.Vibrate(50);
        holdAccelerator = pressed;
        if(pressed && !src.isPlaying)
        {
            src.Play();
        }
        iconAccelerator.color = pressed ? Color.white : Color.black;
    }

    public void Brake(bool pressed){
        Vibration.Vibrate(50);
        holdBrake = pressed;
        iconBrake.color = pressed ? Color.white : Color.black;
    }

    public void TurnLeft(bool pressed)
    {
        
        Vibration.Vibrate(50);
        holdLeft = pressed;
        iconLeft.color = pressed ? Color.white : Color.black;
    }

    public void TurnRight(bool pressed)
    {
        Vibration.Vibrate(50);
        holdRight = pressed;
        iconRight.color = pressed ? Color.white : Color.black;
    }
}
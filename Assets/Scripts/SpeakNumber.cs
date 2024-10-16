﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Dice;
using Systemic.Unity.Pixels;

public class SpeakNumber : MonoBehaviour
{
    public Text numberText;
    public AudioClip[] numbers;
    AudioSource source;

    //Pixel die;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
    }
    // Start is called before the first frame update
    void Start()
    {
        //DicePool.Instance.onDieAvailabilityChanged += OnDieAvailability;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // void OnDieAvailability(Die die, DicePool.DieState oldState, DicePool.DieState newState)
    // {
    //     bool wasConnected = oldState == DicePool.DieState.Ready;
    //     bool isConnected = newState == DicePool.DieState.Ready;
    //     if (!wasConnected && isConnected)
    //     {
    //         this.die = die;

    //         die.OnStateChanged += OnDieStateChanged;
    //     }
    //     else if (wasConnected && !isConnected)
    //     {
    //         if (this.die == die)
    //         {
    //             this.die = null;
    //         }
    //     }
    // }



    void OnDieStateChanged(Pixel die, PixelRollState newState, int newFace)
    {
        numberText.text = (newFace + 1).ToString();
        if (newState == PixelRollState.OnFace)
        {
            Debug.Log("New Face: " + newFace);
            source.PlayOneShot(numbers[newFace]);
        }
    }
}

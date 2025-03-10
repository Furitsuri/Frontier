﻿using UnityEngine;
using System.Collections;

namespace Frontier
{
    public class Loader : MonoBehaviour
    {
        public GameObject gameManager;          //GameManager prefab to instantiate.

        void Awake()
        {
            //Check if a GameManager has already been assigned to static variable GameManager.instance or if it's still null
            if (GameMain.instance == null)
            {
                //Instantiate gameManager prefab
                Instantiate(gameManager);
            }

            //Check if a SoundManager has already been assigned to static variable GameManager.instance or if it's still null
            // if (SoundManager.instance == null)

            //Instantiate SoundManager prefab
            // Instantiate(soundManager);
        }
    }
}
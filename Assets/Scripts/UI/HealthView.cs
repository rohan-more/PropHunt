using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthView : MonoBehaviour
{
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private TMP_Text _playerName;
    private int maxHealth = 10;
    private int currentHealth;
    private void Start()
    {
        _playerName.text = PhotonNetwork.NickName;
        currentHealth = maxHealth;
        if (RoomManager.Instance.GetPlayerType(PhotonNetwork.NickName) == PlayerType.HUNTER)
        {
            this.gameObject.SetActive(false);
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        _healthSlider.value = currentHealth;
    }
}

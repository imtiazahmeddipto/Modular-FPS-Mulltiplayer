using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Realtime;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ScoreboardItem : MonoBehaviourPunCallbacks
{
	public TMP_Text usernameText;
	public TMP_Text killsText;
	public TMP_Text deathsText;

	Player player;

	public void Initialize(Player player)
	{
		this.player = player;

		usernameText.text = player.NickName;
		UpdateStats();
	}

	void UpdateStats()
	{
        int kills = 0;
        if (player.CustomProperties.TryGetValue("kills", out object killsObj))
        {
            kills = (int)killsObj;
        }
        killsText.text = kills.ToString();

        // Get deaths with default value 0
        int deaths = 0;
        if (player.CustomProperties.TryGetValue("deaths", out object deathsObj))
        {
            deaths = (int)deathsObj;
        }
        deathsText.text = deaths.ToString();
    }

	public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
	{
		if(targetPlayer == player)
		{
			if(changedProps.ContainsKey("kills") || changedProps.ContainsKey("deaths"))
			{
				UpdateStats();
			}
		}
	}
}

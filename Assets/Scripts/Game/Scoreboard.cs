using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;

public class Scoreboard : MonoBehaviourPunCallbacks
{
	public bool isInComputer;
    [SerializeField] Transform container;
	[SerializeField] GameObject scoreboardItemPrefab;
	[SerializeField] GameObject Icon;
	[SerializeField] CanvasGroup canvasGroup;
    Dictionary<Player, ScoreboardItem> scoreboardItems = new Dictionary<Player, ScoreboardItem>();

	void Start()
	{
		foreach(Player player in PhotonNetwork.PlayerList)
		{
			AddScoreboardItem(player);
		}
    }

    private void Update()
    {
		if (isInComputer)
		{
			Icon.SetActive(false);
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                canvasGroup.alpha = 1;
            }
            else if (Input.GetKeyUp(KeyCode.Tab))
            {
                canvasGroup.alpha = 0;
            }
        }else
		{
			Icon.SetActive(true);
		}
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		AddScoreboardItem(newPlayer);
	}

	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		RemoveScoreboardItem(otherPlayer);
	}

	void AddScoreboardItem(Player player)
	{
		ScoreboardItem item = Instantiate(scoreboardItemPrefab, container).GetComponent<ScoreboardItem>();
		item.Initialize(player);
		scoreboardItems[player] = item;
	}
	public void FinalScore()
	{
        canvasGroup.alpha = 1;
        canvasGroup.enabled = false;
    }
	void RemoveScoreboardItem(Player player)
	{
		Destroy(scoreboardItems[player].gameObject);
		scoreboardItems.Remove(player);
	}

	public void ShowScore()
	{
canvasGroup.alpha = 1;
	}

	public void HideScore()
	{
canvasGroup.alpha = 0;
	}
}

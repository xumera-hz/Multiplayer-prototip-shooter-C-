using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyMenu : MonoSingleton<LobbyMenu> {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void Active(bool state)
    {
        gameObject.SetActive(state);
    }

    public void ClickExit()
    {
        var client = GameClient.I;
        if (client.IsNullOrDestroy()) return;
        client.Disconnect("You leave from game");
    }
}

OrbPickup.OnTriggerEnter2D(player)
    → publishes OrbCollectedMessage(type, 0)
        → OrbCounter.OnOrbCollected()
            → _total++
            → republishes OrbCollectedMessage(type, _total)
                → OrbCountController.OnOrbCollected()
                    → _countText.text = msg.NewTotal.ToString()



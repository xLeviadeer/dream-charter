[to Overview](./overview.md)

### Woah╌hold on╌this is technical
This content is about changing your Discord presence. You should have an understanding of how Discord presence apps work before continuing.

---
# Discord Presence
Discord presence is the ability to set discord presence when running the program. 

## Info
**The program will need to be opened ⊰before⊱ the game you are playing to show properly.**

## Where to Change It
Discord presence requires `./secrets/discord.txt` to be set. Additionally, Discord presence can be configured with `./data/default_large_image.txt` and `game_name.txt`. 

1. `discord.txt` must contain a valid ⟨Discord Application ID⟩. It should be the only text in the file with no new lines.
2. `default_larg_image.txt` is expected to contain an image ID contained within your Discord app (in the Discord developer portal). It sets the large image that goes with your ⸉playing⸉ status. It should be the only text in the file with no new lines.
3. `game_name.txt` is expected to contain the name of the game you want to be displayed in your ⸉playing⸉ status alongside the Dream Charter. It should be the only text in the file with no new lines.

### Example
The example⊶[discord-presence](./Examples/discord-presence)⊷shows the Discord presence settings being configured. Notably, Application ID is left out. You will need to fill this data in with your Application ID. The default large image ID is also left out as you will need to fill this with what you ID the image in your app.
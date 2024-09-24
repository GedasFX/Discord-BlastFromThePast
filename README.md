# Discord: Blast from the Past

Media preservation and "relive past memories" tool.

![Discord_JJKV20Dfjb](https://github.com/user-attachments/assets/cacf586f-28a6-4a8f-a4e3-33f7b5962f8c)

## Usage - Message Buttons

### Jump

Jumps to the message the image is comming from. NOTE: Due to the use of ephemeral messages, the unshared bot messages will dissapear once clicked when channel has a sufficiently large amount of messages.

### More Please

A shorthand for `/btfp show random`.

### Share [ephmerial only]

Posts the same image to the channel for everyone to see.

### Save [ephmerial only]

Sends the image to the DMs, additionally with the command `/btfp show by-id` instructions for recreation.

## Usage - Commands

This simple plugin exposes 2 slash commands:

### `/btfp import [channel_id {default: current_channel_id}]`

> Requires Admin Permissions.

Scans the specified channel for images (starts all the way at the beginning of the channel), (optionally, configured with `/btfp config save-images`) downloads them to local storage, and takes a note of all messages that has images. This is required to display images, as bots do not have access to the search discord API.

### `/btfp show random [channel_id {default: current_channel_id}]`

Displays a random image which was indexed by `/btfp import`.

### `/btfp show by-id <uuid> [channel_id {default: current_channel_id}]`

Displays a requested image, output is the same as of `/btft show random` except it is not random.

### `/btfp config scheduled-posts [enabled {default: false}] [schedule {default: '0 * * * *'}] [channel_id {default: current_channel_id}]`

> Requires Admin Permissions.

Enables/disables scheduled messages and configures the schedule. Configured via [CRON expression](https://crontab.guru), time is in UTC.

### `/btfp config scheduled-import [enabled {default: false}] [schedule {default: '0 * * * *'}] [channel_id {default: current_channel_id}]`

> Requires Admin Permissions.

Enables/disables scheduled `/btfp import` and configures the schedule. Configured via [CRON expression](https://crontab.guru), time is in UTC.

## Usage - Message Components

Message components can be accessed by right-clicking a messagage and navigating to `Apps` section in the dropdown.

### Apps -> BFTP: Share

Equivalent to `/btfp show by-id` + `Share`, but uuid is taken from the message its trying to share. If a message has multiple images, it will take the first one.

### Apps -> BFTP: Save

Equivalent to `/btfp show by-id` + `Save`, but uuid is taken from the message its trying to share. If a message has multiple images, it will take the first one.

## Usage - Archival

When self-hosting and not using the official bot, you can additionally archive the images you want! 

## Self-Host Installation

```yml
# ./docker-compose.yml

services:
  app:
    image: ghcr.io/gedasfx/discord-blastfromthepast:master
    environment:
      DISCORD_TOKEN: ${DISCORD_TOKEN}
      ARCHIVE_DISCORD_GUILD_IDS: 123,456,789,COMMA_SEPERATED_GUILD_IDS_YOU_WANT_TO_ARCHIVE_IMAGES_OF
    volumes:
      - ./data:/app/data
    restart: unless-stopped
```

Make sure to also update the user of the folder:
```
sudo chown 1654:1654 ./data
```

## Privacy Notice

The bot locally stores `guild_id`, `message_id`, and `attachment_id` of every message with images. It is not enough to reconstruct the images without the bots presence.

All data gets deleted after bot is removed from the Discord Server.

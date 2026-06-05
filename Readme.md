# Argonaut WAD Extractor

## Overview

This contains tools for extracting WAD files for various Argonaut games on PC and PSX.

I have a few goals for this tool and repo.
1. Be able to read and extract all WAD files in Arognaut games that used the Croc 2 based WAD format.
1. Be able to make mods. / Be able to rebuild the extracted assets back into working WADs.
1. Be able to convert between different WAD versions.
1. Be able to make WADs from scratch.

## Compatibility

| Feature \ Source | C2 PC | C2 PSX | C2 PSX Demo DUMMY | C2 PSX JP Demo DUMMY | HP1 | HP2 |
| :--------------- | :---- | ------ | ----------------- | -------------------- | --- | --- |
| Images           |       |        |                   |                      |     |     |
| Audio            |       |        |                   |                      |     |     |
| Strat Models     |       |        |                   |                      |     |     |
| Level Geometry   |       |        |                   |                      |     |     |
| Scripts          |       |        |                   |                      |     |     |

Key:
- C1/2 --> Croc 1/2
- HP1/2 --> Harry Potter 1/2

## Credits and Thanks

Massive credit to OverSurge. The tools they have on their [PS1-Argonaut-Reverse](https://github.com/OverSurge/PS1-Argonaut-Reverse) repo were the foundation for my tools here.

A big thanks to Big Boat LLC for providing details about ASL (Argonaut Strat Language). This allows the extractor to produce code looking much closer to the actual script files.

And of course, thanks to all the fantastic people from Argonaut who made all the amazing games!
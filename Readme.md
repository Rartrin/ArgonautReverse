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

## Usage and Contact

My username is `Rartrin` on basically all platforms. Discord is the easiest way to reach me.

When it comes to the texturess and sounds extracted from these tools, they may be property of Argonaut, not me.

You may generally use the these tools however you wish, with one exception.
These tools are for non-commercial use. Without explicit permission from me, anything released that is made with, or using things produced by this code, should be for free.
I am developing these tools on a staggering budget of $0.00. They are intended to help the fans of the community who love Argonaut's games, not for monetary gain.
If you do have something you want to charge for or are unsure, for example, if you want to make a mod for a game but want new versions released behind a Patreon, contact me.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
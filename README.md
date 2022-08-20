# DiablePhone
Multi-platform .NET client to talk to Diable lights

**Latest:** Added a branch to support "Shiny", a different BLE implementation.

## Project Description
This project contains the Mobile Phone / Tablet code for controlling the lights on a relatively simple LED light kit designed for a diabolo.

It could be modified to suit other props, too.

## Related Projects
The 3d printed enclosure is at https://www.thingiverse.com/thing:4625907 - it isn't the version I'm currently using, but it's moderately recent.

The software for the board I'm using (either the [Adafruit Feather Sense](https://www.adafruit.com/product/4516) or
[Adafruit Feather nRF52840 Express](https://www.adafruit.com/product/4062)) can be found in this Github account under the repository
"[DiableBase](https://github.com/alunmj/DiableBase)".

If you want to build one of your own for a DIY project, please do - it's literally a pair of NeoPixel or similar sticks, soldered to the Feather board,
usually to data pins 5 & 6 (and, naturally, to the GND and 3V lines).

![Here's an early picture of the device on a diabolo](https://user-images.githubusercontent.com/22458124/185769219-7a18504d-cba7-4502-857a-a6f3e8b54498.jpg)Here's an early picture of the device on a diabolo

# ImgSort

Change the filename of png images to allow later automated processing to create reference images to train image recognition tools

Currently the images are 800 pixel wide and 96 pixel high

## split in 9 pieces

```
convert img0500.png -set filename:0 "%[t]" \( +clone -roll +88+0 +clone -roll +88+0 +clone -roll +88+0 +clone -roll +88+0 +clone -roll +88+0 +clone -roll +88+0 +clone -roll +88+0 +clone -roll +96+0 \) -extent 96x96 "%[filename:0]_%d.png"
```

## split in 10 pieces (unused at the moment)

```
convert img0342_m.png -set filename:0 "%[t]" \( +clone -roll +80+0 +clone -roll +80+0 +clone -roll +80+0 +clone -roll +80+0 +clone -roll +80+0 +clone -roll +80+0 +clone -roll +80+0 +clone -roll +72+0 +clone -roll +96+0 \) -extent 96x96 "%[filename:0]_%d.png"
```

## X coordinates of the 9 pieces

| start | end |
|-------|-----|
|   0 |  96 |
|  88 | 184 |
| 176 | 272 |
| 264 | 360 |
| 352 | 448 |
| 440 | 536 |
| 528 | 624 |
| 616 | 712 |
| 704 | 800 |

There is an overlap of 8 pixels between each pair of pieces

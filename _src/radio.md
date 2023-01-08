# Online Radio

Quick links to some of my favorite online radio stations:

| Station | Stream Link | lqvlc link |
| --- | --- | --- |
| [DEF CON Radio](https://somafm.com/defcon/) | [Stream Link](https://somafm.com/defcon64.pls) | [lqvlc Link](lqvlc://somafm.com/defcon64.pls) |
| [Deep Space One](https://somafm.com/deepspaceone/) | [Stream Link](https://somafm.com/deepspaceone64.pls) | [lqvlc Link](lqvlc://somafm.com/deepspaceone64.pls) |
| [Vaporwaves](https://somafm.com/vaporwaves/) | [Stream Link](https://somafm.com/vaporwaves64.pls) | [lqvlc Link](lqvlc://somafm.com/vaporwaves64.pls) |
| [Underground 80s](https://somafm.com/u80s/) | [Stream Link](https://somafm.com/u80s64.pls) | [lqvlc Link](lqvlc://somafm.com/u80s64.pls) |
| [Boot Liquor](https://somafm.com/bootliquor/) | [Stream Link](https://somafm.com/bootliquor64.pls) | [lqvlc Link](lqvlc://somafm.com/bootliquor64.pls) |
| [OG 97.9 Atlanta](https://www.og979.com/) | [Stream Link](https://playerservices.streamtheworld.com/api/livestream-redirect/WWWQH3AAC.aac) | [lqvlc Link](lqvlc://playerservices.streamtheworld.com/api/livestream-redirect/WWWQH3AAC.aac) |
| [WBGO Jazz 88.3 Newark](https://www.wbgo.org) | [Stream Link](https://wbgo.streamguys1.com/wbgo128) | [lqvlc Link](lqvlc://wbgo.streamguys1.com/wbgo128) |
| [KHOL 89.1 Jackson Hole](https://891khol.org/) | [Stream Link](http://peridot.streamguys.com:6010/live.m3u?_ga=2.245952768.525453867.1658096358-1871105295.1658096351) | [lqvlc Link](lqvlc://peridot.streamguys.com:6010/live.m3u?_ga=2.245952768.525453867.1658096358-1871105295.1658096351) | 
| [Yesterweb Radio](https://yesterweb.org/radio/) | [Stream Link](https://radio.yesterweb.org/radio/8000/radio.mp3) | [lqvlc Link](lqvlc://radio.yesterweb.org/radio/8000/radio.mp3) | 
| [98.9 Fox Oldies](https://wgnyfm.com/) | [Stream Link](http://ice64.securenetsystems.net/WGNYFM2) | [lqvlc Link](lqvlc://ice64.securenetsystems.net/WGNYFM2) |
| [aNONRadio](https://anonradio.net/) | [Stream Link](https://anonradio.net:8443/anonradio) | [lqvlc Link](lqvlc://anonradio.net:8443/anonradio) |
| Radio El Salvador 96.9 | [Stream Link](https://streamingcwsradio30.com/8198/stream) | [lqvlc Link](lqvlc://streamingcwsradio30.com/8198/stream) |

Alternatively, use the [online radio playlist](/radio/OnlineRadioPlaylist.m3u) I've created to get access to all of them on [MPV](https://mpv.io/) or [VLC](https://www.videolan.org/vlc/). 

## MPV

```bash
mpv --playlist=http://lqdev.me/radio/OnlineRadioPlaylist.m3u --player-operation-mode=pseudo-gui 
```

## VLC

```bash
vlc http://lqdev.me/radio/OnlineRadioPlaylist.m3u
```

What's an lqvlc link? Check out the blog post on the [lqvlc network protocol](/posts/lqvlc-network-protocol-firefox.html).
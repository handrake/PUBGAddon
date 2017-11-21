# PUBGAddon [![Build Status](https://travis-ci.org/handrake/PUBGAddon.svg?branch=master)](https://travis-ci.org/handrake/PUBGAddon)

<img src="https://i.imgur.com/1kNqXhM.png" width="400">

서버 위치 검색기

오고 가는 UDP 패킷을 이용하여 아마존 서버 IP를 찾고, ping을 출력합니다.

## 다운로드

[v0.0.3](https://github.com/handrake/PUBGAddon/releases/download/0.0.3/PUBGAddOnSetup.msi)

## 필요 프로그램

* [WinPcap](http://www.winpcap.org)
* [.NET Framework 4.5](https://www.microsoft.com/en-us/download/details.aspx?id=30653)
* Microsoft Visual C++ 2010 Redistributable Package [x86](http://www.microsoft.com/en-us/download/details.aspx?id=5555) [x64](http://www.microsoft.com/en-us/download/details.aspx?id=14632)
* [Microsoft Visual C++ 2013 Redistributable Package](https://www.microsoft.com/en-us/download/details.aspx?id=40784)

## 이용법

PUBGAddOnSetup.msi 파일을 다운, 설치를 마치고 PUBGAddon.exe을 실행한 후 *사람들이 총 쏘는 배그 로비에 접근한 다음* 서버 검색 버튼을 누릅니다.

## 주의점

배그 프로그램에서 UDP가 교환되는지 특정하지 않기 때문에 컴퓨터에 UDP를 이용하는 프로그램이 많을 경우 결과가 부정확할 수 있습니다. 배그만 실행한 상태에서 이용하길 권장합니다.

ping은 내용이 빈 Http Request를 통해 404 Response 반응 속도를 보는 것이기 때문에 부정확할 수 있습니다.

현재는 한일/북미 서버에서만 작동합니다.

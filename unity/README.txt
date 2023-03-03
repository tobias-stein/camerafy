===============================================================================
PREREQUISIES
===============================================================================
* Directx 11

===============================================================================
UNITY EDITOR PREREQUISIES
===============================================================================
* Unity version 2019.3+

===============================================================================
TSL/SSL Setup
===============================================================================

Windows:
1. run "Scripts/Create_SSL_Certificate.bat" as admin
2. netsh http add urlacl url=https://*:<port>/ user=everyone
3. netsh http add sslcert ipport=0.0.0.0:1988 appid={<guid>} certhash=<hash> (see mmc certificate under Personal/) 

===============================================================================
Network Configuration
===============================================================================

Server:
Must allow Inbound and Output tcp and udp trafic on configured CamerafyHttpServer port (default: 1988)

Client:
Must allow Inbound and Output tcp and udp trafic on configured CamerafyHttpServer port (default: 1988)


===============================================================================
Troubleshooting
===============================================================================

* make sure firewall settings are in place
* if using a WLAN make sure router is forwarding all ports
* Webrtc seems only working locally if device is actually connected to any network
* if working in editor, make sure the 'Editor run in Background' option is enabled
* make sure apache activemq artemis has enough disc space to allocate queues, otherwise peer connections might fail
* For Chrome browser its helpful to disable the chrome://flags 'Anonymize local IPs exposed by WebRTC' and 'Autoplay policy' 
* Unity UI only working with remote input, if game(window) has focus.
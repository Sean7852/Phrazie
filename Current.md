We are moving to the Phase: Output & Remote for Phrazie.

Our goal is to implement the Mobile Remote (Basic) feature.

1.  Architecture: As defined in Section 10, the communication must use a Local WebSocket (desktop ↔ mobile).
2.  QR Code Generation: Add a QR code generation feature to the LivePerformanceView. This code must encode a unique URL for the local machine, pointing to a small, built-in web server.
3.  Web Interface: Create the web-based remote control application. This interface must be dark, minimal, and non-intrusive.
4.  Functionality: 
* View the current Collection & state.
* Select the next Collection & state.
* Control the playback: play / pause.
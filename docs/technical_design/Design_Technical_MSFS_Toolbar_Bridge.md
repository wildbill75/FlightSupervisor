# Design Technical: MSFS Toolbar WebSocket Bridge

## Overview
The `PanelServerService` provides a lightweight bridge between the C# Desktop application and the MSFS In-Game Toolbar (which runs in an internal browser/CoherentGT environment).

## 📡 Communication Architecture
- **Server**: A self-hosted `HttpListener` running on `http://localhost:5050`.
- **WebSocket**: Full-duplex communication using `System.Net.WebSockets`.
- **Payload**: JSON-serialized objects containing live flight data (SuperScore, Current Phase, Ground Ops progress).

## 🖥️ Toolbar UI (Embedded)
- **HTML/CSS**: A high-contrast, VR-friendly interface designed for legibility at low resolutions.
- **Client JS**: Connects to the local WebSocket and updates the DOM in real-time as Broadcast messages are received from the C# backend.
- **Persistence**: The server runs as a background thread in the main application, ensuring the in-game panel remains functional even if the main UI window is minimized.

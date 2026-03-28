# Design Technical: Core Architecture & Telemetry Bridge

## 1. Overview
The communication bridge between Microsoft Flight Simulator (MSFS) and the Flight Supervisor UI via SimConnect and WASM.

---

## 2. Spécifications de Design Technique

### [BACKEND] SimConnectService.cs / WasmLVarClient.cs
- **WASM Fenix Overrider** : Interfaçage avec `L:S_OH_CALLS_SEATBELTS` pour le Fenix A320.
- **Reconnexion Automatique** : Timer de retry silencieux en cas d'échec de communication.
- **Extension Telemetry** : Support des variables complexes (APU, Packs, Températures, Lumières).

### [FRONTEND] IPC Bridge
- **IPC Spams Prevention** : Filtrage des logs redondants lors des phases de reconnexion.
- **Telemetry Payload** : Optimisation du JSON envoyé au WebView2 pour éviter les boucles de sérialisation.

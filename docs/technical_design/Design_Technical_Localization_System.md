# Design Technical: Localization System (i18n)

## Overview
The `LocalizationService` provides a centralized mechanism for translating UI strings and narrative commentaries between English (Primary) and French (Secondary).

## 🏗️ Design
- **Static Dictionary**: Stores hardcoded translations for common keys (e.g., "Boarding" -> "Embarquement").
- **`CurrentLanguage`**: A global string property ("en" or "fr") that determines which dictionary to use.
- **Methods**:
  - `GetString(key)`: Retrieves a translated string from the dictionary.
  - `Translate(en, fr)`: A shorthand method used frequently in narrative generation (Briefing, Ground Logs) to return the correct version inline.

## 📁 Expansion Plan
- Future iterations will move these dictionaries to external JSON files (e.g., `Lang_EN.json`, `Lang_FR.json`) to allow for community translations without recompiling the application.

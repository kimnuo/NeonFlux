---
name: Unity Android Gradle Build - Remove Unused Plugins
description: Diagnose and fix Unity Android Gradle build failures caused by unused or conflicting third-party plugins (e.g., GoogleMobileAds) that introduce missing transitive dependencies
source: auto-skill
extracted_at: '2026-06-09T15:34:59.946Z'
---

## When to Use
- Unity Android build fails with `Could not find androidx.*:some-library:X.X.X` errors
- Gradle build fails with `AAPT: error: attribute layout_constraintX not found`
- Third-party plugin (ads, analytics, etc.) was imported but is not actually used in code
- Build fails despite adding the missing dependency to gradle templates

## Diagnosis Procedure

### Step 1: Verify if the plugin is actually used
```bash
# Search all C# scripts for references to the plugin
grep -r "GoogleMobileAds\|MobileAds\|BannerView\|InterstitialAd" Assets/01_Scripts/
```
If **zero results** → the plugin is unused and can be safely removed.

### Step 2: Identify the problematic plugin
Look at the Gradle error for clues:
- `gnt_medium_template_view.xml` → Google Mobile Ads native ad template
- `firebase_*` → Firebase SDK
- `applovin_*` → AppLovin SDK
- `ironsource_*` → ironSource SDK

The failing XML layout or missing dependency name usually identifies the plugin.

### Step 3: Find ALL plugin files
```bash
# Search for all files matching the plugin name pattern
dir /s /b *GoogleMobileAds*
# or
find . -name "*GoogleMobileAds*" -o -name "*googlemobileads*"
```

Typical locations:
- `Assets/GoogleMobileAds/` (main plugin folder)
- `Assets/GoogleMobileAds.meta`
- `Assets/Plugins/Android/googlemobileads-unity.aar`
- `Assets/Plugins/Android/googlemobileads-unity.aar.meta`
- `Assets/Plugins/Android/GoogleMobileAdsPlugin.androidlib/`
- `Assets/Plugins/Android/GoogleMobileAdsPlugin.androidlib.meta`
- `Assets/Plugins/Android/GoogleMobileAdsPlugin.androidlib/` (subfolder with AndroidManifest.xml, project.properties, etc.)

### Step 4: Remove ALL plugin files
Delete every file found in Step 3, including `.meta` files.

**Critical**: You must remove **both** the `.aar` library files AND the `.androidlib` project folders. Removing only one leaves orphaned references that still cause build failures.

### Step 5: Clean build artifacts
```bash
rmdir /s /q Library\Bee\Android
```
This forces Unity to regenerate the Gradle project from scratch.

### Step 6: Revert gradle template changes
If you added dependency fixes to `mainTemplate.gradle` or `settingsTemplate.gradle`, revert them to their original state. The unused plugin was the root cause, not a missing dependency.

## Why This Happens

Unity's Android build process:
1. Scans `Assets/Plugins/Android/` for `.aar`, `.androidlib`, and `.jar` files
2. Includes them in the Gradle project regardless of whether code references them
3. The plugin's resources/layouts reference transitive dependencies (e.g., ConstraintLayout)
4. If those dependencies aren't in the repository list or don't exist, the build fails

The plugin is included at the **build system level**, not the **code reference level**. So even if no C# script imports the plugin, Gradle still tries to compile it.

## Common Plugin Locations to Check

| Plugin | Files to Remove |
|--------|-----------------|
| GoogleMobileAds | `Assets/GoogleMobileAds/`, `Assets/Plugins/Android/googlemobileads-unity.aar*`, `Assets/Plugins/Android/GoogleMobileAdsPlugin.androidlib/` |
| Firebase | `Assets/Firebase/`, `Assets/Plugins/Android/com.google.firebase.*.aar` |
| Unity IAP | `Assets/Plugins/UnityPurchasing/` |
| AppsFlyer | `Assets/Plugins/Android/AppsFlyerLib*`, `Assets/AppsFlyer/` |

## Key Considerations

| Aspect | Detail |
|--------|--------|
| **Don't just add dependencies** | Adding `implementation 'androidx.constraintlayout:constraint-layout:2.1.4'` may fail if the Maven repository doesn't serve that artifact — removing the unused plugin is more reliable |
| **Remove .meta files too** | Unity tracks all assets with `.meta` files; orphaned meta files cause import errors |
| **Clean Library folder** | Old Gradle caches in `Library/Bee/Android/` will persist the broken state |
| **Check for nested plugins** | Some plugins ship with sub-plugins (e.g., GoogleMobileAds includes both an `.aar` and an `.androidlib`) |

## Verification Steps
1. After removal, `grep -r "PluginName" Assets/` should return zero results
2. `Library/Bee/Android/` should be deleted or freshly regenerated
3. Build should succeed without adding any extra dependencies to gradle templates
4. APK size should be smaller (unused plugin code removed)

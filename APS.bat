@echo off
adb shell am force-stop com.android.vending
adb shell am start -a android.intent.action.VIEW -d "market://details?id=de.kgv.oberrothenbach"
exit

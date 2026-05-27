---
name: input-manager-wiped
description: Project's default Input Manager axes were cleared by imported asset package
metadata:
  type: project
---

The default Unity Input Manager axes (including Mouse X, Mouse Y, Horizontal, Vertical, etc.) were all removed when the Low Poly Shooter Pack asset was imported. Any input axes needed by scripts must be added manually via Edit > Project Settings > Input Manager.

**Why:** The asset package replaced the default InputManager.asset, wiping all 18 default axes.
**How to apply:** When writing scripts that use `Input.GetAxis()`, verify the corresponding axis exists in the Input Manager. If missing, guide the user to add it manually with correct settings.

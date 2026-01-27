## **Here's how PBS stores the Pushbullet API key**

## **Storage Method**
- **Location**: `%AppData%\PBSMS\settings.dat` (per-user ApplicationData folder)
- **Encryption**: Uses `ProtectedData.Protect()` from Windows DPAPI (Data Protection API)

## **Encryption Level**
The encryption uses **Windows DPAPI** with `DataProtectionScope.CurrentUser`:
- DPAPI typically uses **AES-256** or **3DES** encryption
- Keys are derived from the user's Windows login credentials
- Microsoft manages the encryption keys through the Windows security subsystem

## **Windows Account Linkage**

- Data is encrypted using keys derived from the **user's Windows profile**
- Only the same Windows user account can decrypt it
- It's tied to the user profile, **not the password**

## **Impact of a  user changing their Window's account password**
**No impact** - if a user changes their Windows password, the API key remains accessible because DPAPI encryption is tied to the user's SID (Security Identifier) and profile, not the password itself.

## **Multi-User Scenarios**

**Each user can have their own API key without conflicts:**

1. **Storage is per-user**: Each Windows user has their own `%AppData%` folder:
   - User A: `C:\Users\UserA\AppData\Roaming\PBSMS\settings.dat`
   - User B: `C:\Users\UserB\AppData\Roaming\PBSMS\settings.dat`

2. **Encryption is per-user**: `DataProtectionScope.CurrentUser` ensures User A cannot decrypt User B's API key

3. **Installation location irrelevant**: Whether installed in `C:\Program Files\` or elsewhere doesn't matter - the encrypted settings are always stored in each user's AppData folder, not the program directory

Each user must run `pbsms APIKey=<their_key>` at least once to set up their own encrypted API key.

## **Security of the API Key given PBSMS is Open-Source code**

You may be asking yourself **"Can someone change this open-source code to decrypt and display the Pushbullet API key?"**

The open-source code does **not** enable someone to decrypt another user's API key unless they have the ability to sign on to the user's Windows account on the same computer that the API key was stored.<br><br>
**However**, someone **could** to decrypt the API key if they could sign on to the Windows user account on the same computer on which the API Key was stored.

### **The DPAPI Security model**

The security model of Windows DPAPI (Data Protection API) **does not rely on keeping the decryption code secret**. Instead, it relies on:

1. **Windows User Credentials**: The encryption keys are derived from the user's Windows profile and managed by the Windows security subsystem
2. **Operating System Protection**: Only the Windows OS can access the master keys needed for decryption
3. **User Context**: `DataProtectionScope.CurrentUser` ensures only processes running as that specific Windows user can decrypt

### What the PBSMS code shows

The decryption code:
```csharp
byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
```

This code shows **how** to decrypt, but it will only work when:
- Running on the **same computer as the API Key was originally stored**
- Under the **same Windows user account** that encrypted it

### Attack Scenarios

**Cannot decrypt:**
- Remote attacker with the code and `settings.dat` file
- Different Windows user account on the same computer
- Same Windows username on a different computer

**Can decrypt:**
- Someone already logged into the victim's Windows account

## **Backup and Restore Scenarios**

Finally, you may be asking yourself **"What happens if a backup of the user's computer is restored to another computer?"**

**The API key will NOT be accessible** if a backup is restored to a different computer, even if:
- It's the same username
- It's the same user restoring the backup
- The `settings.dat` file is included in the backup

### Why Restoration Fails

`DataProtectionScope.CurrentUser` in DPAPI uses encryption keys that are derived from:

1. **User's SID** (Security Identifier)
2. **Master keys** stored in the user's profile (`%APPDATA%\Microsoft\Protect\{SID}\`)
3. **Machine-specific entropy** in some implementations

When restoring to a new computer:
- The new user account will have a **different SID** (even with the same username)
- The DPAPI master keys from the backup won't be properly associated with the new profile
- Standard backup/restore tools don't typically migrate DPAPI encryption keys properly

### Result

After restoring to a new computer, when `PBSMS` tries to decrypt the backed-up `settings.dat`, the `ProtectedData.Unprotect()` call will **throw an exception**, and the user will need to re-run:
```
pbsms APIKey=<their_api_key>
```

This is actually a **security feature** - it prevents encrypted secrets from being portable across machines, reducing the risk if backup files are compromised.


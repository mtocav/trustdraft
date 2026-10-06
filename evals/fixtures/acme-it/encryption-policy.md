# Acme IT B.V. — Encryption Policy

_Fictional company used for TrustDraft evals. Version 1.2, owner: IT Manager._

## 1. Scope
Applies to all company-owned laptops, servers, removable media and cloud storage used by Acme IT B.V. employees and contractors.

## 2. Data at rest
- All company laptops use full-disk encryption with BitLocker (XTS-AES 256). Encryption is enforced through Microsoft Intune; non-compliant devices are blocked from company resources.
- Server disks and cloud storage volumes are encrypted with provider-managed keys (AES-256).
- Removable media may only be used when encrypted with BitLocker To Go.

## 3. Data in transit
- External traffic uses TLS 1.2 or higher. TLS 1.0/1.1 are disabled.
- Remote administration happens only over VPN or SSH with key-based authentication.

## 4. Key management
- Recovery keys are escrowed in Microsoft Entra ID and accessible only to the IT Manager and one deputy.
- Keys are rotated when a device is reissued or an administrator leaves.

## 5. Review
This policy is reviewed annually.

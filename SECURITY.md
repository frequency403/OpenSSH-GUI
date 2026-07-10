# Security Policy

##  Our Commitment

OpenSSH-GUI is designed with security as a core principle. We take vulnerabilities seriously and aim to handle responsible disclosures transparently.

---

##  Reporting a Vulnerability

### Preferred: GitHub Private Security Advisory
1. Go to [Security Tab](https://github.com/frequency403/OpenSSH-GUI/security/advisories)
2. Click "Report a vulnerability"
3. Submit details via private form

### Alternative: Email
Send to: [frequency403@noreply.github.com](mailto:frequency403@noreply.github.com)

**Include:**
- Affected version(s)
- Steps to reproduce
- Potential impact
- Suggested fix (if known)

**Response Timeline:**

| Severity | Initial Response | ETA for Fix |
| :--------- | :------------------: | -------------: |
| 🔴 Critical | Within 48 hours | ASAP (hotfix) |
| 🟠 High | Within 7 days | 1-2 weeks |
| 🟡 Medium | Within 14 days | Next release |
| 🟢 Low | Within 30 days | Scheduled |

---

## What This Tool Does (and Doesn't)

**Does:**
- Manage SSH keys stored **locally** on your machine
- Parse key metadata **without transmitting data**
- Integrate with system SSH agent (ssh-agent/agent)

**Does NOT:**
- Store keys in cloud/storage servers
- Transmit keys or credentials over network
- Replace OpenSSH security model
- Handle SSH server-side security

**Scope:**  
This tool operates on the **client side only**. Network-level SSH security remains governed by the underlying OpenSSH implementation.

---

## Security Updates

Stay informed by:
- Watching the [Releases](https://github.com/frequency403/OpenSSH-GUI/releases) page
- Enabling dependency updates (Dependabot enabled)
- Following GitHub notifications

---

## Best Practices for Users

1. **Keep OpenSSH-GUI updated** — Security patches are released regularly
2. **Verify checksums** — Download releases from official GitHub Releases only
3. **Use strong passphrases** — GUI doesn't weaken key encryption
4. **Audit installed keys** — Regularly review authorized_keys/known_hosts

---

## Acknowledgments

Security researchers who responsibly disclose vulnerabilities will be credited in release notes (unless anonymity requested).

**Thank you for helping keep OpenSSH-GUI secure!** 
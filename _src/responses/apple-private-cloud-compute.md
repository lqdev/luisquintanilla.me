---
title: "Apple - Private Cloud Compute"
targeturl: https://security.apple.com/blog/private-cloud-compute/
response_type: reshare
dt_published: "2024-06-11 20:42"
dt_updated: "2024-06-11 20:42 -05:00"
tags: ["ai","pcc","privacy","cloud","privatecloudcompute","wwdc"]
---

> We set out to build Private Cloud Compute with a set of core requirements:  
> <br>
> 1. **Stateless computation on personal user data.** ...we want a strong form of stateless data processing where personal data leaves no trace in the PCC system.  
> 2. **Enforceable guarantee.** Security and privacy guarantees are strongest when they are entirely technically enforceable, which means it must be possible to constrain and analyze all the components that critically contribute to the guarantees of the overall Private Cloud Compute system.
> 3. **No privileged runtime access.** Private Cloud Compute must not contain privileged interfaces that would enable Apple’s site reliability staff to bypass PCC privacy guarantees, even when working to resolve an outage or other severe incident.
> 4. **Non-targetability.** An attacker should not be able to attempt to compromise personal data that belongs to specific, targeted Private Cloud Compute users without attempting a broad compromise of the entire PCC system. 
> 5. **Verifiable transparency.** Security researchers need to be able to verify, with a high degree of confidence, that our privacy and security guarantees for Private Cloud Compute match our public promises. 

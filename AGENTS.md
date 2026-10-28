# Contributor identities

The user assigned these GitHub identities to the project contributors:

| Contributor | GitHub account | Commit email |
| --- | --- | --- |
| Codex 2 / XR systems | `businessteen` | `businessteen@users.noreply.github.com` |
| Claude | `Build-with-SK` | `Build-with-SK@users.noreply.github.com` |
| Codex 1 | `lavanya` | `lavanya@users.noreply.github.com` |

Use the identity assigned to your role, not the repository owner's identity by default.
These usernames are supplied by the user; if an assigned label is not the intended GitHub
login, obtain the correct login before changing that contributor's attribution.

Set the commit author and committer using command-scoped Git configuration, or local
configuration in your own isolated checkout. Do not change global Git configuration.
For example, Codex 2 commits with:

```sh
git -c user.name=businessteen \
    -c user.email=businessteen@users.noreply.github.com \
    commit -m "Describe the change"
```

The email must be associated with the intended GitHub account for GitHub to attribute
the commit. The addresses above use GitHub's username-based noreply format; if an account
requires its numeric-ID noreply address, use the address from that account's email settings.

Commit attribution does not select the authenticated account used to push. Each agent's
GitHub connection must use the assigned account if the user also wants separate push
actors. Keep the platform Git proxy configuration intact. This checkout cannot switch
the GitHub connections of other agent sessions.

Do not rewrite existing published commits just to change attribution unless the user
explicitly requests that history change. Codex 2 works on `codex/xr-systems` and delivers
prefabs and integration instructions without editing Codex 1's scenes.

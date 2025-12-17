@echo off
set GIT_EDITOR=echo
set GIT_PAGER=
git add appsettings.json
git commit -m "Remove SendGrid API key from appsettings.json"
git rebase --continue


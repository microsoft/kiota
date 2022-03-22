git config pull.rebase false
git config push.default simple
git config remote.origin.prune true
git config commit.gpgsign true
git config tag.gpgsign true
git config core.safecrlf false
currentPath = $PWD
cd /workspace/kiota
gh repo clone microsoft/kiota-samples samples
cd $currentPath
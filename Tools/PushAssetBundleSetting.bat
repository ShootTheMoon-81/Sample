@echo off

rem git add
git add ../Assets/AddressableAssetsData/*

rem git commit
git commit -m JenkinsBuild_develop

rem git push
git push origin develop

exit
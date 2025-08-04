#!/bin/bash
echo "PushAssetBundleSetting"

#git add
git add ../Assets/AddressableAssetsData/*

#git commit
git commit -m JenkinsBuildiOS_develop

#git push
git push origin develop

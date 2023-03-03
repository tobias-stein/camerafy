@echo off

pushd "frontend"
npm run serve -- --host %1 --port %2
popd
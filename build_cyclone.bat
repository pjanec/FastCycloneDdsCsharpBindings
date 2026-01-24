pushd %~dp0cyclonedds
mkdir build
cd build
cmake -A x64 -DCMAKE_INSTALL_PREFIX=%~dp0cyclone-compiled ..
cmake --build . --target install --config Release
popd
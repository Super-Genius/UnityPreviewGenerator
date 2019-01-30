pushd ../wiki
pandoc -V geometry:margin=.5in Logo.md Home.md Installation.md Editor-Window-\&-Preview-Component.md Camera-Manipulation.md Property-Settings.md Sprite-Renderer-Code-Sample-Explanation.md _Footer.md --pdf-engine=xelatex -o ../Documentation/PreviewGenerator.pdf
popd

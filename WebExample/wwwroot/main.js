// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import {dotnet} from './_framework/dotnet.js'

const {setModuleImports, getAssemblyExports, getConfig} = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

setModuleImports('main.js', {
    window: {
        location: {
            href: () => globalThis.window.location.href
        }
    }
});

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
let createSVG = () => {
    alert("viz not loaded");
};
window["vizInstance"] = null;
function compile()
{
    const res = exports.WebRegExCompiler.RegExToNfaAndDfa(document.getElementById("RegExInput").value);
    document.getElementById('outNfa').innerHTML = res[0];
    document.getElementById('outDfa').innerHTML = res[1];
    createSVG();
}
document.getElementById("renderButton").onclick = () => {
    compile();
};

document.getElementById("RegExInput").oninput = ()=>
{
    if(document.getElementById("ConstantCompile").checked)
    {
        compile();
    }
}

Viz.instance().then(viz => {
    console.log("Viz loaded");
    window["vizInstance"] = viz;
    const setSVG = (src, dest) => {
        if (document.getElementById("TB").checked) {
            src = src.replace("rankdir=LR;", "rankdir=TB;")
        }
        const svg = viz.renderSVGElement(src);
        svg.removeAttribute("width");
        svg.removeAttribute("height");
        document.getElementById(dest).appendChild(svg);
    }
    createSVG = () => {
        document.getElementById("NFA").innerHTML = "";
        document.getElementById("DFA").innerHTML = "";
        setSVG(document.getElementById("outNfa").innerText, "NFA");
        setSVG(document.getElementById("outDfa").innerText, "DFA");
    }
});

await dotnet.run();
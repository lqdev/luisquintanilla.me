function redirect(pageUrl){
    let path = window.location.pathname;
    window.location = `${pageUrl}${path}`
}
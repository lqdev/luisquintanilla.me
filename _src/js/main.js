function redirect(pageUrl){
    window.location = `${pageUrl}/${window.location.pathname.split('/').slice(1).join('/')}`
}
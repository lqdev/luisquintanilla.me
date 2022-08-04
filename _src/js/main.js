function redirect(pageUrl){
    let path = 
        window.location.pathname
            .split('/')
            .slice(1)
            .join('/');
    window.location = `${pageUrl}/${path}`
}
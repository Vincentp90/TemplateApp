import http from 'k6/http';
import { sleep, check } from 'k6';

const APIURL = "http://localhost/api"
//const APIURL = "http://localhost:5186"

export const options = {
    vus: 20,
    duration: '20s',
};

const defaultParams = {
    headers: {
        'Content-Type': 'application/json',
        'X-Forwarded-Proto': 'https'
    }
};

let authToken = null;
let authHeaders = null;

function login() {
    const res = http.post(APIURL + '/auth/login', JSON.stringify({
        username: `k6user${__VU}@example.com`,
        password: 'k6userpassword'
    }), defaultParams);

    check(res, {
        'login ok': r => r.status === 200,
    });

    const cookieHeader = res.headers['Set-Cookie'];
    let token = null;
    if (cookieHeader) {
        const match = cookieHeader.match(/auth_token=([^;]+);/);
        if (match) token = match[1];
    }
    return token;
}

const searchTerms = ["Battle", "Duty", "World", "War", "Sim", "Dragon", "Snow", "Team"];
function searchAndAdd(){
    const term = searchTerms[Math.floor(Math.random() * searchTerms.length)];
    const res = http.get(APIURL + `/applisting/search/${term}`, { headers: authHeaders });
    check(res, { 'search ok': res => res.status === 200 });
    //Take a random result from res
    const data = JSON.parse(res.body);
    const randomresult = data[Math.floor(Math.random() * data.length)];

    //console.log("Adding " + randomresult.name);

    const addRes = http.post(APIURL + `/wishlist/${randomresult.appid}`, null, { headers: authHeaders });
    check(addRes, { 'add ok': res => res.status === 200 });
}

function deleteIfMoreThanTen(){
    const res = http.get(APIURL + `/wishlist?fields=appid,name`, { headers: authHeaders });
    check(res, { 'get wishlist ok': res => res.status === 200 });
    const data = JSON.parse(res.body);
    if (data.length <= 10) {
        return;
    }
    const toDeleteCount = Math.floor(Math.random() * 3) + 1;
    for (let i = 0; i < toDeleteCount; i++) {
        const randomItem = data[Math.floor(Math.random() * data.length)];

        const delRes = http.del(`${APIURL}/wishlist/${randomItem.appid}`, null, { headers: authHeaders });
        check(delRes, { 'delete ok': r => r.status === 200 });

        //console.log(`Deleted ${randomItem.name} (${randomItem.appid})`);
    }
}


export default function () {
    if (__ITER === 0) {
        //we assume we are already registered, if not run authme first with equal or higher VUs
        //register();
        authToken = login();
    }

    if (!authToken) {
        fail("Login failed");
    }
    authHeaders = {
        'Content-Type': 'application/json',
        'Cookie': `auth_token=${authToken}`,
    };

    searchAndAdd();
    deleteIfMoreThanTen()
    sleep(1);
}
import http from 'k6/http';
import { sleep, check } from 'k6';

const APIURL = "http://localhost:5186"

export const options = {
    vus: 200,
    duration: '30s',
};

const defaultParams = {
    headers: {
        'Content-Type': 'application/json',
        'X-Forwarded-Proto': 'https'
    }
};

function register() {
    const res = http.post(APIURL + '/auth/register', JSON.stringify({
        username: `k6user${__VU}@example.com`,
        password: 'k6userpassword'
    }), defaultParams);
    check(res, {
        'register ok': r => r.status === 200,
    });
    return res;
}

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

let authToken = null;
export default function () {
    // Perform login once per VU
    let token = null;
    if (__ITER === 0) {
        register();
        authToken = login();
    }

    if (!authToken) {
        return;
    }
    const authHeaders = {
        'Content-Type': 'application/json',
        'Cookie': `auth_token=${authToken}`,
    };
    const res = http.get(APIURL + '/auth/me', { headers: authHeaders });
    check(res, { 'secure ok': res => res.status === 200 });

    sleep(1);
}
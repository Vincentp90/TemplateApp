'use client';

import { useSuspenseQuery } from "@tanstack/react-query";
import { api } from "../api";
import { Link } from "@tanstack/react-router";

type UserDetails = {
    rowVersion: number;
    email: string;
    firstName: string | null;
    lastName: string | null;
    country: string | null;
    city: string | null;
    address: string | null;
};

export default function Profile() {
    const { data: userDetails } = useSuspenseQuery<UserDetails>({
        queryKey: ['userDetails'],
        queryFn: async () => {
            const res = await api.get("/user");
            const data = res.data;
            return data;
        },
    });

    return (
        <div className="max-w-3xl mx-auto p-6">
            <h2 className="text-2xl font-semibold mb-6">User Profile</h2>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">                
                <Field label="First Name" value={userDetails.firstName} />
                <Field label="Last Name" value={userDetails.lastName} />
                <Field label="Email" value={userDetails.email} />
                <Field label="Country" value={userDetails.country} />
                <Field label="City" value={userDetails.city} />
                <Field label="Address" value={userDetails.address} />
            </div>

            <div className="mt-8 flex justify-end">
                <Link to="/app/profile/edit" className="block hover:text-gray-300">Edit</Link>
            </div>
        </div>
    );
}

function Field({ label, value }: { label: string; value: string | null }) {
    return (
        <div className="flex flex-col bg-white shadow rounded-xl p-4 border border-gray-200">
            <span className="text-sm font-medium text-gray-600">{label}</span>
            <span className="mt-1 text-gray-900">{value ?? ""}</span>
        </div>
    );
}
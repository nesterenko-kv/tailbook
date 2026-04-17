"use client";

import { FormEvent, useEffect, useState } from "react";
import { apiRequest, ApiError } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import type { PermissionItem, RoleItem, UserListItem } from "@/lib/types";
import { Badge, Card, ErrorBanner, Field, Input, PageHeader, PrimaryButton, SuccessBanner, TextArea } from "@/components/ui";

export default function IamPage() {
  const [users, setUsers] = useState<UserListItem[]>([]);
  const [roles, setRoles] = useState<RoleItem[]>([]);
  const [permissions, setPermissions] = useState<PermissionItem[]>([]);
  const [selectedUserId, setSelectedUserId] = useState("");
  const [roleCodes, setRoleCodes] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [createForm, setCreateForm] = useState({ email: "", displayName: "", password: "", roleCodes: "" });

  async function loadAll() {
    setError(null);
    try {
      const [userResponse, roleResponse, permissionResponse] = await Promise.all([
        apiRequest<UserListItem[]>("/api/admin/iam/users"),
        apiRequest<RoleItem[]>("/api/admin/iam/roles"),
        apiRequest<PermissionItem[]>("/api/admin/iam/permissions")
      ]);
      setUsers(userResponse);
      setRoles(roleResponse);
      setPermissions(permissionResponse);
    } catch (err) { setError(err instanceof ApiError ? err.message : "Failed to load IAM data."); }
  }

  useEffect(() => { void loadAll(); }, []);

  async function createUser(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null); setSuccess(null);
    try {
      await apiRequest(`/api/admin/iam/users`, { method: "POST", body: JSON.stringify({ email: createForm.email, displayName: createForm.displayName, password: createForm.password, roleCodes: createForm.roleCodes.split(',').map(x=>x.trim()).filter(Boolean) }) });
      setSuccess("User created.");
      setCreateForm({ email: "", displayName: "", password: "", roleCodes: "" });
      await loadAll();
    } catch (err) { setError(err instanceof ApiError ? err.message : "Failed to create user."); }
  }

  async function assignRoles(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!selectedUserId) return;
    setError(null); setSuccess(null);
    try {
      await apiRequest(`/api/admin/iam/users/${selectedUserId}/roles`, { method: "POST", body: JSON.stringify({ id: selectedUserId, roleCodes: roleCodes.split(',').map(x=>x.trim()).filter(Boolean) }) });
      setSuccess("Roles assigned.");
      await loadAll();
    } catch (err) { setError(err instanceof ApiError ? err.message : "Failed to assign roles."); }
  }

  return (
    <div className="flex flex-col gap-6 px-2 py-2">
      <PageHeader eyebrow="IAM" title="Identity and access" description="Manage users, roles and permissions for admin/groomer/client access surfaces." />
      <ErrorBanner message={error} />
      <SuccessBanner message={success} />
      <div className="grid gap-6 xl:grid-cols-[1.2fr_1fr]">
        <Card title="Users">
          <div className="grid gap-3">
            {users.map((user) => (
              <button key={user.id} type="button" onClick={() => { setSelectedUserId(user.id); setRoleCodes(user.roles.join(', ')); }} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 text-left transition hover:border-emerald-500/40">
                <div className="flex items-start justify-between gap-3"><div><div className="font-medium">{user.displayName}</div><div className="text-sm text-slate-400">{user.email}</div><div className="text-xs text-slate-500">Created {formatDateTime(user.createdAtUtc)}</div></div><Badge>{user.status}</Badge></div>
                <div className="mt-3 flex flex-wrap gap-2">{user.roles.map(role => <Badge key={role}>{role}</Badge>)}</div>
              </button>
            ))}
          </div>
        </Card>
        <div className="grid gap-6">
          <Card title="Create user">
            <form className="grid gap-4" onSubmit={createUser}>
              <Field label="Email"><Input type="email" value={createForm.email} onChange={(e)=>setCreateForm(c=>({...c, email:e.target.value}))} required /></Field>
              <Field label="Display name"><Input value={createForm.displayName} onChange={(e)=>setCreateForm(c=>({...c, displayName:e.target.value}))} required /></Field>
              <Field label="Password"><Input type="password" value={createForm.password} onChange={(e)=>setCreateForm(c=>({...c, password:e.target.value}))} required /></Field>
              <Field label="Initial role codes"><Input value={createForm.roleCodes} onChange={(e)=>setCreateForm(c=>({...c, roleCodes:e.target.value}))} placeholder="admin, manager" /></Field>
              <PrimaryButton type="submit">Create user</PrimaryButton>
            </form>
          </Card>
          <Card title="Assign roles">
            <form className="grid gap-4" onSubmit={assignRoles}>
              <Field label="User id"><Input value={selectedUserId} onChange={(e)=>setSelectedUserId(e.target.value)} placeholder="Pick from user list or paste id" /></Field>
              <Field label="Role codes"><Input value={roleCodes} onChange={(e)=>setRoleCodes(e.target.value)} placeholder="admin, groomer" /></Field>
              <PrimaryButton type="submit">Assign roles</PrimaryButton>
            </form>
          </Card>
          <Card title="Available roles and permissions">
            <div className="space-y-4 text-sm">
              <div>
                <h3 className="font-medium text-white">Roles</h3>
                <div className="mt-2 grid gap-2">{roles.map(role => <div key={role.id} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-3"><div className="font-medium">{role.code}</div><div className="text-slate-400">{role.displayName}</div><div className="mt-2 flex flex-wrap gap-2">{role.permissionCodes.map(code => <Badge key={code}>{code}</Badge>)}</div></div>)}</div>
              </div>
              <div>
                <h3 className="font-medium text-white">Permissions</h3>
                <div className="mt-2 flex flex-wrap gap-2">{permissions.map(permission => <Badge key={permission.id}>{permission.code}</Badge>)}</div>
              </div>
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}

"use client";

import { FormEvent, useEffect, useState } from "react";
import { ShieldOff } from "lucide-react";
import { apiRequest, ApiError } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import type { MeResponse, PagedResult, PermissionItem, ResetMfaRecoveryResponse, RoleItem, UserListItem } from "@/lib/types";
import { Badge, Card, ErrorBanner, Field, Input, PageHeader, PrimaryButton, SuccessBanner } from "@/components/ui";

const MFA_RECOVERY_WRITE_PERMISSION = "iam.mfa.recovery.write";

export default function IamPage() {
  const [users, setUsers] = useState<UserListItem[]>([]);
  const [roles, setRoles] = useState<RoleItem[]>([]);
  const [permissions, setPermissions] = useState<PermissionItem[]>([]);
  const [currentUser, setCurrentUser] = useState<MeResponse | null>(null);
  const [selectedUserId, setSelectedUserId] = useState("");
  const [roleCodes, setRoleCodes] = useState("");
  const [mfaResetResult, setMfaResetResult] = useState<ResetMfaRecoveryResponse | null>(null);
  const [isResettingMfa, setIsResettingMfa] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [createForm, setCreateForm] = useState({
    email: "",
    displayName: "",
    password: "",
    roleCodes: ""
  });

  async function loadAll() {
    setError(null);

    try {
      const [meResponse, userResponse, roleResponse, permissionResponse] = await Promise.all([
        apiRequest<MeResponse>("/api/identity/me"),
        apiRequest<PagedResult<UserListItem>>("/api/admin/iam/users?page=1&pageSize=100"),
        apiRequest<RoleItem[]>("/api/admin/iam/roles"),
        apiRequest<PermissionItem[]>("/api/admin/iam/permissions")
      ]);

      setCurrentUser(meResponse);
      setUsers(userResponse.items);
      setRoles(roleResponse);
      setPermissions(permissionResponse);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to load IAM data.");
    }
  }

  useEffect(() => {
    void loadAll();
  }, []);

  async function createUser(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    try {
      await apiRequest(`/api/admin/iam/users`, {
        method: "POST",
        body: JSON.stringify({
          email: createForm.email,
          displayName: createForm.displayName,
          password: createForm.password,
          roleCodes: createForm.roleCodes
            .split(",")
            .map((x) => x.trim())
            .filter(Boolean)
        })
      });

      setSuccess("User created.");
      setCreateForm({ email: "", displayName: "", password: "", roleCodes: "" });
      await loadAll();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to create user.");
    }
  }

  async function assignRoles(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!selectedUserId) return;

    setError(null);
    setSuccess(null);

    try {
      await apiRequest(`/api/admin/iam/users/${selectedUserId}/roles`, {
        method: "POST",
        body: JSON.stringify({
          id: selectedUserId,
          roleCodes: roleCodes
            .split(",")
            .map((x) => x.trim())
            .filter(Boolean)
        })
      });

      setSuccess("Roles assigned.");
      await loadAll();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to assign roles.");
    }
  }

  async function resetMfaRecovery() {
    if (!selectedUserId) return;

    setIsResettingMfa(true);
    setError(null);
    setSuccess(null);
    setMfaResetResult(null);

    try {
      const response = await apiRequest<ResetMfaRecoveryResponse>(`/api/admin/iam/users/${selectedUserId}/mfa/recovery/reset`, {
        method: "POST"
      });

      setMfaResetResult(response);
      setSuccess("MFA recovery state reset.");
      await loadAll();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to reset MFA recovery state.");
    } finally {
      setIsResettingMfa(false);
    }
  }

  const selectedUser = users.find((user) => user.id === selectedUserId) ?? null;
  const canResetMfaRecovery = currentUser?.permissions.includes(MFA_RECOVERY_WRITE_PERMISSION) ?? false;
  const isSelfReset = Boolean(currentUser && selectedUserId === currentUser.userId);
  const canSubmitMfaReset = canResetMfaRecovery && Boolean(selectedUserId) && !isSelfReset && !isResettingMfa;

  return (
    <div className="flex flex-col gap-6 px-2 py-2">
      <PageHeader
        eyebrow="IAM"
        title="Identity and access"
        description="Manage users, roles and permissions for admin, groomer and client access surfaces."
      />

      <ErrorBanner message={error} />
      <SuccessBanner message={success} />

      <div className="grid gap-6 xl:grid-cols-[1.2fr_1fr]">
        <Card title="Users">
          <div className="grid gap-3">
            {users.map((user) => (
              <button
                key={user.id}
                type="button"
                onClick={() => {
                  setSelectedUserId(user.id);
                  setRoleCodes(user.roles.join(", "));
                  setMfaResetResult(null);
                }}
                className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 text-left transition hover:border-emerald-500/40"
              >
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <div className="font-medium">{user.displayName}</div>
                    <div className="text-sm text-slate-400">{user.email}</div>
                    <div className="text-xs text-slate-500">
                      Created {formatDateTime(user.createdAt)}
                    </div>
                  </div>
                  <Badge>{user.status}</Badge>
                </div>

                <div className="mt-3 flex flex-wrap gap-2">
                  {user.roles.map((role) => (
                    <Badge key={role}>{role}</Badge>
                  ))}
                </div>
              </button>
            ))}
          </div>
        </Card>

        <div className="grid gap-6">
          <Card title="Create user">
            <form className="grid gap-4" onSubmit={createUser}>
              <Field label="Email">
                <Input
                  type="email"
                  value={createForm.email}
                  onChange={(e) =>
                    setCreateForm((current) => ({ ...current, email: e.target.value }))
                  }
                  required
                />
              </Field>

              <Field label="Display name">
                <Input
                  value={createForm.displayName}
                  onChange={(e) =>
                    setCreateForm((current) => ({
                      ...current,
                      displayName: e.target.value
                    }))
                  }
                  required
                />
              </Field>

              <Field label="Password">
                <Input
                  type="password"
                  value={createForm.password}
                  onChange={(e) =>
                    setCreateForm((current) => ({ ...current, password: e.target.value }))
                  }
                  required
                />
              </Field>

              <Field label="Initial role codes">
                <Input
                  value={createForm.roleCodes}
                  onChange={(e) =>
                    setCreateForm((current) => ({ ...current, roleCodes: e.target.value }))
                  }
                  placeholder="admin, manager"
                />
              </Field>

              <PrimaryButton type="submit">Create user</PrimaryButton>
            </form>
          </Card>

          <Card title="Assign roles">
            <form className="grid gap-4" onSubmit={assignRoles}>
              <Field label="User id">
                <Input
                  value={selectedUserId}
                  onChange={(e) => {
                    setSelectedUserId(e.target.value);
                    setMfaResetResult(null);
                  }}
                  placeholder="Pick from user list or paste id"
                />
              </Field>

              <Field label="Role codes">
                <Input
                  value={roleCodes}
                  onChange={(e) => setRoleCodes(e.target.value)}
                  placeholder="admin, groomer"
                />
              </Field>

              <PrimaryButton type="submit">Assign roles</PrimaryButton>
            </form>
          </Card>

          {canResetMfaRecovery ? (
            <Card title="MFA recovery reset" description="Disable another user's enabled MFA factors and invalidate active recovery codes and outstanding challenges.">
              <div className="grid gap-4">
                <div className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 text-sm">
                  <div className="text-slate-400">Selected user</div>
                  <div className="mt-1 font-medium text-white">{(selectedUser?.displayName ?? selectedUserId) || "None selected"}</div>
                  {selectedUser ? <div className="mt-1 text-slate-500">{selectedUser.email}</div> : null}
                </div>

                {isSelfReset ? (
                  <div className="rounded-2xl border border-amber-700/40 bg-amber-500/10 px-4 py-3 text-sm text-amber-200">
                    Admin recovery reset cannot be used on your own account.
                  </div>
                ) : null}

                <PrimaryButton
                  type="button"
                  disabled={!canSubmitMfaReset}
                  onClick={() => void resetMfaRecovery()}
                  className="inline-flex w-full items-center justify-center gap-2 !bg-rose-500 !text-white hover:!bg-rose-400"
                >
                  <ShieldOff aria-hidden="true" className="size-4" />
                  <span>{isResettingMfa ? "Resetting..." : "Reset MFA recovery"}</span>
                </PrimaryButton>

                {mfaResetResult ? (
                  <div className="grid gap-3 text-sm sm:grid-cols-3">
                    <div className="rounded-2xl border border-slate-800 bg-slate-950 px-3 py-3">
                      <div className="text-xs uppercase tracking-[0.2em] text-slate-500">Factors</div>
                      <div className="mt-2 text-xl font-semibold text-white">{mfaResetResult.disabledFactorCount}</div>
                    </div>
                    <div className="rounded-2xl border border-slate-800 bg-slate-950 px-3 py-3">
                      <div className="text-xs uppercase tracking-[0.2em] text-slate-500">Codes</div>
                      <div className="mt-2 text-xl font-semibold text-white">{mfaResetResult.invalidatedRecoveryCodeCount}</div>
                    </div>
                    <div className="rounded-2xl border border-slate-800 bg-slate-950 px-3 py-3">
                      <div className="text-xs uppercase tracking-[0.2em] text-slate-500">Challenges</div>
                      <div className="mt-2 text-xl font-semibold text-white">{mfaResetResult.invalidatedChallengeCount}</div>
                    </div>
                    <div className="rounded-2xl border border-slate-800 bg-slate-950 px-3 py-3 sm:col-span-3">
                      <div className="text-xs uppercase tracking-[0.2em] text-slate-500">Reset</div>
                      <div className="mt-2 font-medium text-white">{formatDateTime(mfaResetResult.resetAt)}</div>
                    </div>
                  </div>
                ) : null}
              </div>
            </Card>
          ) : null}

          <Card title="Available roles and permissions">
            <div className="space-y-4 text-sm">
              <div>
                <h3 className="font-medium text-white">Roles</h3>
                <div className="mt-2 grid gap-2">
                  {roles.map((role) => (
                    <div
                      key={role.id}
                      className="rounded-2xl border border-slate-800 bg-slate-950/60 p-3"
                    >
                      <div className="font-medium">{role.code}</div>
                      <div className="text-slate-400">{role.displayName}</div>
                      <div className="mt-2 flex flex-wrap gap-2">
                        {role.permissionCodes.map((code) => (
                          <Badge key={code}>{code}</Badge>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              <div>
                <h3 className="font-medium text-white">Permissions</h3>
                <div className="mt-2 flex flex-wrap gap-2">
                  {permissions.map((permission) => (
                    <Badge key={permission.id}>{permission.code}</Badge>
                  ))}
                </div>
              </div>
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}

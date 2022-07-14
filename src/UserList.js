import React from "react";

function UserList({ users = [] }) {
  return (
    <div className="user-list">
      <div className="user-list-header">
        <input className="form-control" type="text" />
        <button className="user-list-header-button btn btn-secondary">
          フィルタ
        </button>
      </div>

      <div className="user-list-grid-header">
        <div className="user-list-grid-item">ユーザー名</div>
        <div className="user-list-grid-item">メールアドレス</div>
      </div>
      <div className="user-list-grid">
        {users.map((user) => (
          <React.Fragment key={user.id}>
            <div className="user-list-grid-item">{user.name}</div>
            <div className="user-list-grid-item">"@@@@@"</div>
          </React.Fragment>
        ))}
      </div>
    </div>
  );
}

export default UserList;
